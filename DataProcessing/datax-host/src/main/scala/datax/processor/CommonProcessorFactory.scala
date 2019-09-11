// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.processor

import java.sql.Timestamp
import java.util.concurrent.Executors

import com.microsoft.azure.eventhubs.EventData
import datax.executor.ExecutorHelper
import datax.config._
import datax.constants._
import datax.data.FileInternal
import datax.exception.EngineException
import datax.fs.HadoopClient
import datax.host.{AppHost, CommonAppHost, SparkSessionSingleton, UdfInitializer}
import datax.input.{BlobPointerInput, InputManager, SchemaFile, StreamingInputSetting}
import datax.sink.{BlobSinker, OutputManager, OutputOperator}
import datax.telemetry.{AppInsightLogger, MetricLoggerFactory}
import datax.utility._
import datax.handler._
import datax.input.BlobPointerInput.{inputPathToInternalProps, pathHintsFromBlobPath}
import datax.sql.TransformSQLParser
import org.apache.kafka.clients.consumer.ConsumerRecord
import org.apache.log4j.LogManager
import org.apache.spark.rdd.RDD
import org.apache.spark.sql.{DataFrame, Row, SparkSession}
import org.apache.spark.sql.functions._
import org.apache.spark.sql.streaming.{OutputMode, Trigger}
import org.apache.spark.storage.StorageLevel

import scala.language.{postfixOps, reflectiveCalls}
import scala.collection.mutable.{HashMap, HashSet, ListBuffer}
import scala.concurrent.{Await, ExecutionContext, Future}
import scala.concurrent.duration._
import scala.collection.JavaConverters._

/*
  Generate the common processor
 */
object CommonProcessorFactory {
  private val threadPool = Executors.newFixedThreadPool(8)
  implicit private val ec = ExecutionContext.fromExecutorService(threadPool)
  val appHost = CommonAppHost

  /*
    Create the processor based on input config, initialize all functions to be used in streaming iterations
   */
  def createProcessor(config: UnifiedConfig):CommonProcessor = {
    val sparkConf = config.sparkConf
    val spark = appHost.getSpark(sparkConf)

    val dict = config.dict
    import spark.implicits._

    // Load and initialize functions in parallel to be used in streaming iterations.
    val loadings = Tuple5(
      Future{SchemaFile.loadInputSchema(dict)},
      ProjectionHandler.loadProjectionsFuture(dict),
      TransformHandler.loadTransformFuture(dict),
      ReferenceDataHandler.loadReferenceDataFuture(spark, dict),
      Future {ExtendedUDFHandler.initialize(spark, dict)}
    )

    val (rawBlockSchema, projections, sqlParsed, referencedDataLoaded, udfs) =
      Await.result(for {
        r1 <- loadings._1
        r2 <- loadings._2
        r3 <- loadings._3
        r4 <- loadings._4
        r5 <- loadings._5
      } yield (r1, r2, r3, r4, r5), 10 minutes)

    BuiltInFunctionsHandler.initialize(spark, dict)
    ProjectionHandler.validateProjections(projections)
    JarUDFHandler.loadJarUdf(spark, dict)
    AzureFunctionHandler.initialize(spark, dict)
    UdfInitializer.initialize(spark, dict)
    val createdTables = StateTableHandler.createTables(spark, dict)
    val inputNormalizer = InputNormalizerHandler.initialize(spark, dict)
    val inputNormalizerUdf = if(inputNormalizer==null) udf((s:String)=>s) else udf(inputNormalizer)
    val preProjection = PreProjectionHandler.initialize(spark, dict)
    val buildPropertiesUdf = PropertiesHandler.initialize(spark, dict)

    /*
      function parse input string into a Raw object column based on the input raw blob schema and also project the data frame
       based on the columns from projection files
     */
    def project(inputDf: DataFrame, batchTime: Timestamp): DataFrame = {
      // Initial schema and data set
      var df = inputDf
        .withColumn(ColumnName.RawObjectColumn, from_json(inputNormalizerUdf(col(ColumnName.RawObjectColumn)), rawBlockSchema))

      df = if(preProjection==null)df else preProjection(df)
      val preservedColumns = df.schema.fieldNames.filter(_.startsWith(ColumnName.InternalColumnPrefix))
      df = df.withColumn(ColumnName.PropertiesColumn, buildPropertiesUdf(col(ColumnName.InternalColumnFileInfo), lit(batchTime)))

      for(step <- 0 until projections.length)
        df = df.selectExpr(projections(step)++preservedColumns: _*)

      df
    }

    // initialize metrics settings
    val metricAppName = dict.getMetricAppName()
    val metricConf = MetricsHandler.getMetricsSinkConf(dict)

    // figure out how to output
    val outputs = OutputManager.getOperatators(dict)
    val outputCount = outputs.size

      // initialize output handlers
      outputs.par.foreach(o => {
        if (o.onInitialization != null)
          o.onInitialization(spark)
      })

    // initialize settings of time windows
    val timewindows = TimeWindowHandler.initialize(spark, dict)

    // store past RDDs for overlapping time windows
    val pastRdds = new HashMap[Timestamp, RDD[Row]]()

    // store names of data frames for outputting
    val dataframes = new HashMap[String, DataFrame]

    /*
      function to execute the queries for transforming data frames and output them accordingly
     */
    def route(projectedDf: DataFrame, batchTime: Timestamp, batchInterval: Duration, outputPartitionTime: Timestamp, targets: Set[String], tableNamespace:String) = {
      val transformLogger = LogManager.getLogger("Transformer")

      // store the mapping information between table names in the query and the actual data frame name we are processing
      val tableNames = new HashMap[String, String]

      // register the start input table for the query
      val initTableName = DatasetName.DataStreamProjection+tableNamespace
      tableNames(DatasetName.DataStreamProjection) = initTableName
      dataframes += initTableName -> projectedDf

            // store the data frames that we should unpersist after one iteration
      val dataFramesToUncache = new ListBuffer[DataFrame]
      transformLogger.warn("Persisting the current projected dataframe")
      projectedDf.persist()
      dataFramesToUncache += projectedDf

      // store the metrics to send after one iteration
      val outputMetrics = HashMap[String, Double]()

      // log metric of how many events are incoming on this iteration
      val inputRawEventsCount = projectedDf.count()
      transformLogger.warn(s"Received $inputRawEventsCount raw input events")
      outputMetrics += s"Input_Normalized_Events_Count" -> inputRawEventsCount

      if(timewindows.isEnabled){
        // when time window is turned on, we need to calculate the start and end time of the window and query against
        // the past RDD from history
        // we calculate the time window as the maximum time span from the time windows passed-in from settings
        // this one below determines the end time as event filter, note to minus the watermark span which is the buffer for events to finalize
        val windowEndTime = Timestamp.from(batchTime.toInstant.minusSeconds(timewindows.watermark.toSeconds))
        // this one below determines the start time filter, which is maximum start time of all time windows
        val windowStartTime = Timestamp.from(windowEndTime.toInstant.minusSeconds(timewindows.maxWindow.toSeconds))
        val rdd = projectedDf.where(timewindows.timestampColumn>=windowEndTime).rdd

        transformLogger.warn("Persisting the windowed projected data frame")
        rdd.persist(StorageLevel.MEMORY_ONLY_SER)

        // log metric of after filtered by the time window, how many events actually are participating in the transformation
        val inputEventsCount = rdd.mapPartitions(it=>{
          val loggerSuffix = SparkEnvVariables.getLoggerSuffix()
          val instrumentLogger = LogManager.getLogger(s"${ProductConstant.ProductInstrumentLogger}$loggerSuffix")
          val t1 = System.nanoTime()
          instrumentLogger.warn(s"Start collecting events at $t1")
          val count = it.toArray.length
          val timeNow = System.nanoTime()
          instrumentLogger.warn(s"Collected $count events for caching, spent time=${(timeNow-t1)/1E9} seconds")
          Iterator.single(count)
        }).reduce(_+_)

        transformLogger.warn(s"Received $inputEventsCount input events for ${initTableName}")
        outputMetrics += s"Input_${DatasetName.DataStreamProjection}_Events_Count" -> inputEventsCount

        // collect data from past RDDs that fits in the time window
        val cutTime = Timestamp.from(batchTime.toInstant.minusSeconds((timewindows.watermark+timewindows.maxWindow).toSeconds))
        pastRdds.keys.filter(_.compareTo(cutTime)<=0).foreach(k=>{
          pastRdds.remove(k) match {
            case Some(rdd) =>
              transformLogger.warn(s"removing past RDD at ${k} since it is before or equal to ${cutTime}")
              rdd.unpersist(false)
            case None =>
              transformLogger.warn(s"Unexpectedly ${k} does exist in the pastRDDs")
          }
        })

        // union the data from current projected data frame and the past ones
        val sc = rdd.sparkContext
        val pastDataUnion = spark.createDataFrame(if(pastRdds.size>1){
          transformLogger.warn(s"union ${pastRdds.size} batches, including ${pastRdds.keySet.mkString(",")}")
          sc.union(rdd, pastRdds.values.toSeq: _*)
        } else rdd, projectedDf.schema)

        val unionTableNameInSql = DatasetName.DataStreamProjectionWithWindow
        val unionTableName = unionTableNameInSql+tableNamespace
        pastDataUnion
          .where(timewindows.timestampColumn>=windowStartTime && timewindows.timestampColumn<windowEndTime)
          .createOrReplaceTempView(unionTableName)
        tableNames(unionTableNameInSql) = unionTableName
        dataframes(unionTableName)=spark.table(unionTableName)

        // register time-windowed tables and their corresponding data frames for different time window spec
        for (tw <- timewindows.windows) {
          val winTableName = tw._1
          val winTableNameInScope = winTableName + tableNamespace
          val winStartTime = Timestamp.from(windowEndTime.toInstant.minusSeconds(tw._2.toSeconds))
          transformLogger.warn(s"Create or replace time windowed view '${winTableNameInScope}' within window('$winStartTime' - '$windowEndTime')")
          pastDataUnion
            .where(timewindows.timestampColumn>=winStartTime && timewindows.timestampColumn<windowEndTime)
            .createOrReplaceTempView(winTableNameInScope)
          tableNames(winTableName) = winTableNameInScope
          dataframes(winTableNameInScope)=spark.table(winTableNameInScope)
        }

        // replace the starting table
        val adjustedBatchStartTime = Timestamp.from(windowEndTime.toInstant.minusSeconds(batchInterval.toSeconds))
        val cachedProjectedDf = pastDataUnion.where(timewindows.timestampColumn>=adjustedBatchStartTime && timewindows.timestampColumn<windowEndTime)
        cachedProjectedDf.createOrReplaceTempView(initTableName)

        // register a table to reference to the projected data frame within only the current iteration batch
        val batchedTableName = DatasetName.DataStreamProjectionBatch + tableNamespace
        projectedDf.createOrReplaceTempView(batchedTableName)
        tableNames(DatasetName.DataStreamProjectionBatch) = batchedTableName
        dataframes(batchedTableName)=projectedDf

        pastRdds(batchTime) = rdd
      }
      else{
        // if time window is not turned on, we simply register the projected data frame as input starting table for query
        outputMetrics += s"Input_${DatasetName.DataStreamProjection}_Events_Count" -> inputRawEventsCount
        projectedDf.createOrReplaceTempView(initTableName)
      }

      // register state-store tables
      for (elem <- createdTables) {
        tableNames(elem._1)=elem._2.getActiveTableName()
      }

      // start executing queries
      if(sqlParsed!=null && sqlParsed.commands.length>0){
        val partitionNumber = projectedDf.rdd.getNumPartitions
        val queries = sqlParsed.commands
        queries.foreach(expr=>{
          val statement = TransformSQLParser.replaceTableNames(expr.text, tableNames)
          expr.commandType match {
            case TransformSQLParser.CommandType_Command =>
              transformLogger.warn(s"Executing command '$statement'")
              spark.sql(statement)
            case TransformSQLParser.CommandType_Query =>
              createdTables.find(_._1 == expr.name) match {
                case Some(t) =>
                  // this case is a query statement assigns data set back to a registered state-store table
                  // so we have to overwrite the existing state-store table with the new data
                  t._2.overwrite(statement)
                  tableNames(t._1) = t._2.flip()
                case None =>
                  // this is a normal case that we borther to handle state-store tables
                  val tableName = expr.name + tableNamespace
                  transformLogger.warn(s"Creating view '$tableName' for '$statement'")

                  val ds = if(partitionNumber > 0) {
                    spark.sql(statement).coalesce(partitionNumber)
                  }
                  else {
                    transformLogger.warn(s"Zero events found for $tableName' for '$statement'")
                    spark.sql(statement)
                  }

                  tableNames(expr.name) = tableName
                  dataframes(tableName) = ds

                  // cache data frames which has been referenced more than once to improve performance
                  if(TransformHandler.shouldCacheCommonViews(dict) && sqlParsed.viewReferenceCount.getOrElse(expr.name, 0)>1){
                    transformLogger.warn(s"Caching view '$tableName' for it would be used more than once")
                    ds.cache()
                    dataFramesToUncache += ds
                  }

                  ds.createOrReplaceTempView(tableName)
              }
            case _ =>
              throw new EngineException(s"unknown commandType : ${expr.commandType}")
          }
        })
      }

      // start outputting data
      def outputHandler(operator: OutputOperator) = {
        val tableName = operator.name
        val outputTableName = tableName+tableNamespace
        dataframes.get(outputTableName) match {
          case None => throw new EngineException(s"could not find data set name '$outputTableName' for output '${operator.name}'")
          case Some(df) =>
            if (operator.onBatch != null) operator.onBatch(df.sparkSession, outputPartitionTime, targets)
            operator.output(df, outputPartitionTime).map { case (k, v) => (s"Output_${operator.name}_" + k) -> v.toDouble }
        }
      }

      var result = Map.empty[String,Double]
      if(outputCount>0) {
        // if there are multiple outputs, we kick off them in parallel
        result = if (outputCount > 1)
          outputs.par.map(outputHandler).reduce(_ ++ _)
        else
          outputHandler(outputs(0))
      }

      // persisting state-store tables
      for (elem <- createdTables) {
        elem._2.persist()
      }

      // clear cache of the data frames in this batch of iteration
      transformLogger.warn("Un-persisting the dataframes")
      dataFramesToUncache.foreach(_.unpersist(false))
      dataFramesToUncache.clear()

      outputMetrics ++ result
    }

    /*
      function to process unified data frame - which has 4 columns: raw string input, Properties, SystemProperties and an internal metadata column for processing
     */
    def processDataset(data: DataFrame,
                       batchTime: Timestamp,
                       batchInterval: Duration,
                       outputPartitionTime: Timestamp,
                       targets: Set[String],
                       namespace: String):Map[String, Double] = {
      val t1 = System.nanoTime()
      val batchLogger = LogManager.getLogger(ProductConstant.DataStreamProcessDataSetLogger)
      val metricLogger = MetricLoggerFactory.getMetricLogger(metricAppName, metricConf)
      val spark = data.sparkSession

      def postMetrics(metrics: Iterable[(String, Double)]): Unit = {
        batchLogger.warn(s"Sending metrics:\n${metrics.map(m => m._1 + " -> " + m._2).mkString("\n")}")
        metricLogger.sendBatchMetrics(metrics, batchTime.getTime)
      }

      try{
        // call ExtendedUDFs to refresh their data
        udfs.foreach(udf=>{
          if(udf._2!=null)udf._2(spark, batchTime)
        })

        // if raw input is specified in the output settings as one of the output, we cache it and register that to allow it to be output
        val persistRaw = outputs.find(p=>p.name==DatasetName.DataStreamRaw).isDefined
        if(persistRaw){
          data.cache()
          dataframes(DatasetName.DataStreamRaw) = data
        }

        // main processing steps
        val baseProjection = project(data, batchTime)
        val counts = route(baseProjection, batchTime, batchInterval, outputPartitionTime, targets, namespace)

        // clear the cache of raw input table if needed.
        if(persistRaw){
          data.unpersist(false)
        }

        // calculate performance metrics
        val partitionProcessedTime = System.nanoTime
        val latencyInSeconds = (DateTimeUtil.getCurrentTime().getTime - batchTime.getTime)/1000D
        val metrics = Map[String, Double](
          "Latency-Process" -> (partitionProcessedTime - t1) / 1E9,
          "Latency-Batch" -> latencyInSeconds
        ) ++ counts

        postMetrics(metrics)
        metrics
      }
      catch{
        case e: Exception =>
          appHost.getTelemetryService().trackEvent(ProductConstant.ProductRoot + "/error", Map(
            "errorLocation" -> "ProcessDataFrame",
            "errorMessage" -> e.getMessage,
            "errorStackTrace" -> e.getStackTrace.take(10).mkString("\n"),
            "batchTime" -> batchTime.toString
          ), null)
          appHost.getTelemetryService().trackException(e, Map(
            "errorLocation" -> "ProcessDataFrame",
            "errorMessage" -> e.getMessage,
            "batchTime" -> batchTime.toString
          ), null)

          Thread.sleep(1000)
          throw e
      }
    }

    CommonProcessor(
      /*
        process a batch of EventData from EventHub
       */
      processEventData = (rdd: RDD[EventData], batchTime: Timestamp, batchInterval: Duration, outputPartitionTime: Timestamp) =>{
        processDataset(rdd
          .map(d=>{
            val bodyBytes = d.getBytes
            if(bodyBytes==null) throw new EngineException(s"null bytes from event: ${d.getObject}, properties:${d.getProperties}, systemProperties:${d.getSystemProperties}")
            (
              new String(bodyBytes),
              d.getProperties.asScala.map{case(k,v)=>k->v.toString},
              if(d.getSystemProperties!=null) d.getSystemProperties.asScala.map{case(k,v)=>k->v.toString} else Map.empty[String, String],
              FileInternal())
          })
          .toDF(
            ColumnName.RawObjectColumn,
            ColumnName.RawPropertiesColumn,
            ColumnName.RawSystemPropertiesColumn,
            ColumnName.InternalColumnFileInfo
          ), batchTime, batchInterval, outputPartitionTime, null, "")
      },

      /*
        process structured streaming for given data frame
        Note this is incomplete and not used for now
       */
      processEventHubDataFrame = (df: DataFrame) => {
        val logger = LogManager.getLogger("processEventHubDataFrame")
        df
          .select(
            from_json(col("body").cast("string"), rawBlockSchema).alias("Raw"),
            col("properties"),
            col("enqueuedTime")
          )
          .selectExpr("Raw.*", "properties", "enqueuedTime")
          .withWatermark("enqueuedTime", "60 seconds")
          .createOrReplaceTempView(DatasetName.DataStreamProjection)
        val outputs = sqlParsed.commands
          .filter(n=>n.commandType==TransformSQLParser.CommandType_Query).map(n=>n.name->n.text)
          .toMap

        val streamingConf = StreamingInputSetting.getStreamingInputConf(dict)
        val interval = streamingConf.intervalInSeconds

        outputs.map{case(k, v)=>{
          k-> spark.sql(v).writeStream
            .outputMode(OutputMode.Append())
            .format("console")
            .trigger(Trigger.ProcessingTime(interval, SECONDS))
            .start()
        }}
      },

      /*
        process json data frame
       */
      processJson = (jsonRdd: RDD[String], batchTime: Timestamp, batchInterval: Duration, outputPartitionTime: Timestamp) =>{
        processDataset(jsonRdd.map((FileInternal(), _)).toDF(ColumnName.InternalColumnFileInfo, ColumnName.RawObjectColumn),
          batchTime, batchInterval, outputPartitionTime, null, "")
      },
      // process blob path from batch blob input
      processBatchBlobPaths = (pathsRDD: RDD[String],
                      batchTime: Timestamp,
                      batchInterval: Duration,
                      outputPartitionTime: Timestamp,
                      namespace: String) => {


       val spark = SparkSessionSingleton.getInstance(pathsRDD.sparkContext.getConf)

       val metricLogger = MetricLoggerFactory.getMetricLogger(metricAppName, metricConf)
       val batchTimeStr = DateTimeUtil.formatSimple(batchTime)
       val batchLog = LogManager.getLogger(s"BatchProcessor-B$batchTimeStr")
       val batchTimeInMs = batchTime.getTime

       def postMetrics(metrics: Iterable[(String, Double)]): Unit = {
          metricLogger.sendBatchMetrics(metrics, batchTime.getTime)
          batchLog.warn(s"Metric ${metrics.map(m => m._1 + "=" + m._2).mkString(",")}")
        }

       batchLog.warn(s"Start batch ${batchTime}, output partition time:${outputPartitionTime}, namespace:${namespace}")
       val t1 = System.nanoTime

        // Wrap files to FileInternal object
       val internalFiles =  pathsRDD.map(file => {
                            FileInternal(inputPath = file,
                              outputFolders = null,
                              outputFileName = null,
                              fileTime = null,
                              ruleIndexPrefix = "",
                              target = null
                            )
                          })

        val paths = internalFiles.map(_.inputPath).collect()
        postMetrics(Map(s"InputBlobs" -> paths.size.toDouble))
        batchLog.warn(s"InputBlob count=${paths.size}");

        val blobStorageKey = ExecutorHelper.createBlobStorageKeyBroadcastVariable(paths.head, spark)

        val inputDf = internalFiles
                      .flatMap(file => HadoopClient.readHdfsFile(file.inputPath, gzip = file.inputPath.endsWith(".gz"), blobStorageKey)
                      .filter(l=>l!=null && !l.isEmpty).map((file, outputPartitionTime, _)))
                      .toDF(ColumnName.InternalColumnFileInfo, ColumnName.MetadataColumnOutputPartitionTime, ColumnName.RawObjectColumn)

        val processedMetrics = processDataset(inputDf, batchTime, batchInterval, outputPartitionTime, null, "")

        val batchProcessingTime = (System.nanoTime - t1) / 1E9

        val metrics = Map[String, Double](
          "BatchProcessedET" -> batchProcessingTime
        )
        processedMetrics ++ metrics
      },

      // process blob path pointer data frame
      processPaths = (pathsRDD: RDD[String],
                      batchTime: Timestamp,
                      batchInterval: Duration,
                      outputPartitionTime: Timestamp,
                      namespace: String) => {
        val spark = SparkSessionSingleton.getInstance(pathsRDD.sparkContext.getConf)

        val metricLogger = MetricLoggerFactory.getMetricLogger(metricAppName, metricConf)
        val batchTimeStr = DateTimeUtil.formatSimple(batchTime)
        val batchLog = LogManager.getLogger(s"BatchProcessor-B$batchTimeStr")

        // Functions used with in processPaths
        val batchTimeInMs = batchTime.getTime

        def postMetrics(metrics: Iterable[(String, Double)]): Unit = {
          metricLogger.sendBatchMetrics(metrics, batchTimeInMs)
          batchLog.warn(s"Metric ${metrics.map(m => m._1 + "=" + m._2).mkString(",")}")
        }

        // Process the array of input files, and sink them
        // Return metrics: (number of processed blobs, number of processed events, number of filtered events sent to eventhub)
        def processBlobs(files: Array[FileInternal],
                         outputPartitionTime: Timestamp,
                         partition: String,
                         targetPar: String): Map[String, Double] = {
          val filesCount = files.length
          val t1 = System.nanoTime()

          // Get the earliest blob to calculate latency
          val paths = files.map(_.inputPath)
          val blobTimes = files.map(_.fileTime).filterNot(_ == null).toList

          postMetrics(Map(s"InputBlobs" -> filesCount.toDouble))

          val (minBlobTime, maxBlobTime) =
            if(blobTimes.length>0) {
              val minBlobTime = blobTimes.minBy(_.getTime)
              val maxBlobTime = blobTimes.maxBy(_.getTime)
              batchLog.warn(s"partition '$partition': started, size: $filesCount, blob time range[${DateTimeUtil.formatSimple(minBlobTime)}, ${DateTimeUtil.formatSimple(maxBlobTime)}]")
              (minBlobTime, maxBlobTime)
            }
            else{
              batchLog.warn(s"Cannot figure out timestamp from file name, please check if there is misconfiguration in the fileTimeRegex setting")
              (null, null)
            }

          val pathsList = paths.mkString(",")
          batchLog.debug(s"Batch loading files:$pathsList")
          val inputDf = spark.sparkContext.parallelize(files, filesCount)
            .flatMap(file => HadoopClient.readHdfsFile(file.inputPath, gzip = file.inputPath.endsWith(".gz"))
              .filter(l=>l!=null && !l.isEmpty).map((file, outputPartitionTime, _)))
            .toDF(ColumnName.InternalColumnFileInfo, ColumnName.MetadataColumnOutputPartitionTime, ColumnName.RawObjectColumn)

          val targets = files.map(_.target).toSet
          val processedMetrics = processDataset(inputDf, batchTime, batchInterval, outputPartitionTime, targets, partition)
          if(minBlobTime!=null){
            val latencyInSeconds = (DateTimeUtil.getCurrentTime().getTime - minBlobTime.getTime)/1000D
            val latencyMetrics = Map(s"Latency-Blobs" -> latencyInSeconds)
            postMetrics(latencyMetrics)
            latencyMetrics++processedMetrics
          }
          else{
            processedMetrics
          }
        }

        def processPartition(v: (String, HashSet[FileInternal])) = {
          val par = v._1
          val paths = v._2.toArray
          processBlobs(paths, outputPartitionTime, par+namespace, par)
        }

        batchLog.warn(s"Start batch ${batchTime}, output partition time:${outputPartitionTime}, namespace:${namespace}")
        val t1 = System.nanoTime
        val pathsGroups = BlobPointerInput.pathsToGroups(rdd = pathsRDD,
          jobName = dict.getAppName(),
          dict = dict,
          outputTimestamp = outputPartitionTime)
        val pathsFilteredGroups = BlobPointerInput.filterPathGroups(pathsGroups)
        val pathsCount = pathsFilteredGroups.aggregate(0)(_ + _._2.size, _ + _)
        //try {
        val result =
          if (pathsCount > 0) {
            batchLog.warn(s"Loading filtered blob files count=$pathsCount, First File=${pathsFilteredGroups.head._2.head}")
            if (pathsFilteredGroups.length > 1)
              Await.result(FutureUtil.failFast(pathsFilteredGroups
                .map(kv => Future {
                  processPartition(kv)
                })), 5 minutes).reduce(DataMerger.mergeMapOfDoubles)
            else
              processPartition(pathsFilteredGroups(0))
          }
          else {
            batchLog.warn(s"No valid paths is found to process for this batch")
            Map[String, Double]()
          }
        val batchProcessingTime = (System.nanoTime - t1) / 1E9

        val metrics = Map[String, Double](
          "BatchProcessedET" -> batchProcessingTime
        )

        postMetrics(metrics)
        batchLog.warn(s"End batch ${batchTime}, output partition time:${outputPartitionTime}, namespace:${namespace}")

        metrics ++ result
      }, // end of processPaths
      /*
      process a batch of ConsumerRecords from kafka
     */
      processConsumerRecord = (rdd: RDD[ConsumerRecord[String,String]], batchTime: Timestamp, batchInterval: Duration, outputPartitionTime: Timestamp) =>{
        processDataset(rdd
          .map(d=>{
            val value = d.value()
            if(value==null) throw new EngineException(s"null bytes from ConsumerRecord")
            //Capture key if present. Key can be null.
            val key = if(d.key!=null) Some("key"->d.key.toString) else None
            (
              value,
              Map.empty[String, String],// Properties
              Map[String, String](
                "offset"->d.offset().toString,
                "partition"->d.partition().toString,
                "serializedKeySize"->d.serializedKeySize().toString,
                "serializedValueSize"->d.serializedValueSize().toString,
                "timestamp"->d.timestamp().toString,
                "timestampType"->d.timestampType().toString,
                "topic"->d.topic()
              ) ++ key,
              FileInternal())
          })
          .toDF(
            ColumnName.RawObjectColumn,
            ColumnName.RawPropertiesColumn,
            ColumnName.RawSystemPropertiesColumn,
            ColumnName.InternalColumnFileInfo
          ), batchTime, batchInterval, outputPartitionTime, null, "")
      } // end of proessConsumerRecord
    ) // end of CommonProcessor
  } // end of init


} // end of CommonProcessorFactory
