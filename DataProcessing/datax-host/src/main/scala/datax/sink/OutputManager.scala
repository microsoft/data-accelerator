// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.sink

import java.sql.Timestamp

import datax.config.{SettingDictionary, SettingNamespace, SparkEnvVariables}
import datax.constants.{ColumnName, MetricName, ProductConstant}
import datax.data.FileInternal
import datax.exception.EngineException
import datax.fs.HadoopClient
import datax.utility.{DataMerger, DataNormalization, SinkerUtil}
import org.apache.log4j.LogManager
import org.apache.spark.TaskContext
import org.apache.spark.sql.functions.{col, struct, to_json}
import org.apache.spark.sql.types.StructType
import org.apache.spark.sql.{DataFrame, Row, SparkSession}


object OutputManager {
  val NamespacePrefix = SettingNamespace.JobOutputPrefix
  val SettingOutputProcessedSchemaPath = "processedschemapath"

  val sinkFactories = Seq[SinkOperatorFactory](
    BlobSinker, EventHubStreamPoster, HttpPoster, CosmosDBSinkerManager
  ).map(f=>f.getSettingNamespace()->f).toMap

  def getOperatators(dict: SettingDictionary): Seq[OutputOperator] ={
    dict.groupBySubNamespace(NamespacePrefix)
      .map(g=>generateOperator(g._2, g._1))
      .toSeq
  }

  def outputResultReducer = (s1: (Int, Map[String, Int]), s2: (Int, Map[String, Int])) => (s1._1 + s2._1, DataMerger.mergeMapOfCounts(s1._2, s2._2))

  def generateOperator(dict:SettingDictionary, name: String) = {
    val logger = LogManager.getLogger("OutputOperatorsBuilder")
    var flagColumnIndex = 1

    val processedSchemaPath = dict.get(SettingOutputProcessedSchemaPath).orNull
    val sinkOperators = dict
        .groupBySubNamespace()
        .map { case (k, v) => sinkFactories.get(k).map(_.getSinkOperator(v, k))}
      .filter(o => o match {
        case Some(oper) =>
          logger.info(s"Output '$name':${oper.name} is ${SinkerUtil.boolToOnOff(oper.isEnabled)}")
          oper.isEnabled
        case None => false
      }).map(o=>{
        val oper = o.get
        val flagColumnExpr = oper.flagColumnExprGenerator()
        if(flagColumnExpr==null){
          logger.warn(s"Output type:'${oper.name}': no flag column")
          (oper.name, (null, null), oper.generator(-1), oper.onBatch, oper.onInitialization)
        }
        else{
          val appendColumn = (flagColumnExpr, s"_${ProductConstant.ProductOutputFilter}_${oper.name}")
          logger.warn(s"Output type:'${oper.name}': append column:$appendColumn")
          flagColumnIndex+=1
          (oper.name, appendColumn, oper.generator(flagColumnIndex), oper.onBatch, oper.onInitialization)
        }
      }).toSeq

    if(sinkOperators.length==0)throw new EngineException(s"no sink is defined for output '$name'!")
    logger.warn(s"Output '$name' to ${sinkOperators.length} sinkers: ${sinkOperators.map(s=>s"'${s._1}'").mkString(",")}")

    val flagColumns = sinkOperators.map(_._2).filter(_._1!=null)
    val sinkers = sinkOperators.map(o=>o._1 -> o._3).toMap
    val onBatchHandlers = sinkOperators.map(_._4).filter(_!=null)
    val onInitHandlers = sinkOperators.map(_._5).filter(_!=null)
    var shouldGeneratorProcessedSchema = processedSchemaPath!=null && !processedSchemaPath.isEmpty

    OutputOperator(
      name = name,
      onInitialization = if(onInitHandlers.size>0) (spark: SparkSession)=> for (elem <- onInitHandlers) {elem(spark)} else null,
      onBatch = if(onBatchHandlers.size>0) (spark:SparkSession, time: Timestamp, targets: Set[String]) => {
        onBatchHandlers.foreach(_(spark, time, targets))
      } else null,
      output = (df: DataFrame, partitionTime: Timestamp) => {
        val outputLogger = LogManager.getLogger(s"Output-${name}")
        val outputColumns = df.schema.filterNot(_.name.startsWith(ColumnName.InternalColumnPrefix)).toArray
        if(shouldGeneratorProcessedSchema){
          val spark = df.sparkSession
          spark.synchronized{
            if(shouldGeneratorProcessedSchema){
              HadoopClient.writeHdfsFile(processedSchemaPath, new StructType(outputColumns).prettyJson, true)
              outputLogger.warn(s"Saved processed schema to $processedSchemaPath")
              shouldGeneratorProcessedSchema = false
            }
          }
        }

        val outputColumnNames = outputColumns.map(c=>DataNormalization.sanitizeColumnName(c.name))
        outputLogger.warn(s"Output fields: ${outputColumnNames.mkString(",")}")

        sink(df, outputColumnNames, partitionTime, flagColumns, sinkers)
      }
    )
  }

  def sink(df: DataFrame,
           outputFieldNames: Seq[String],
           partitionTime: Timestamp,
           flagColumns: Seq[(String, String)],
           outputOperators: Map[String, SinkDelegate]) = {
    val amendDf = if(df.schema.fieldNames.contains(ColumnName.InternalColumnFileInfo))df
    else {
      df.withColumn(ColumnName.InternalColumnFileInfo, FileInternal.udfEmptyInternalInfo())
    }

    val query = amendDf.selectExpr("*" +: flagColumns.map(c => c._1 + " AS " + c._2): _*)
      .select(Seq(col(ColumnName.InternalColumnFileInfo), to_json(struct(outputFieldNames.map(col): _*))) ++
        flagColumns.map(_._2).map(col): _*)

    //query.explain will dump the execution plan of sql to stdout
    //query.explain(true)
    query
      .rdd
      .mapPartitions(it => {
        val partitionId = TaskContext.getPartitionId()
        val loggerSuffix = SparkEnvVariables.getLoggerSuffix()
        val logger = LogManager.getLogger(s"EventsSinker${loggerSuffix}")
        //val path = outputFileFolder+"/part-"+tc.partitionId().toString + ".json.gz"

        val t1 = System.nanoTime()
        var timeLast = t1
        var timeNow: Long = 0
        logger.info(s"$timeNow:Partition started")

        val dataAll = it.toArray
        val count = dataAll.length
        timeNow = System.nanoTime()
        logger.info(s"$timeNow:Collected $count events, spent time=${(timeNow - timeLast) / 1E9} seconds")
        timeLast = timeNow

        val inputMetric = Map(s"${MetricName.MetricSinkPrefix}InputEvents" -> count)
        Seq(if (count > 0) {
          val rowInfo = dataAll(0).getAs[Row](0)
          if(outputOperators.size==0)
            throw new EngineException("no output operators are found!")
          outputOperators
            .par
            .map(_._2(rowInfo, dataAll, partitionTime, partitionId, loggerSuffix))
            .reduce(DataMerger.mergeMapOfCounts) ++ inputMetric
        }
        else
          inputMetric
        ).iterator
      })
      .reduce(DataMerger.mergeMapOfCounts)
  }
}
