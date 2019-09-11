// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

package datax.sink

import java.sql.Timestamp
import java.text.SimpleDateFormat

import datax.executor.ExecutorHelper
import datax.config._
import datax.constants.{JobArgument, MetricName}
import datax.data.{FileInternal, ProcessResult}
import datax.fs.HadoopClient
import datax.securedsetting.KeyVaultClient
import datax.sink.BlobOutputSetting.BlobOutputConf
import datax.utility.{GZipHelper, SinkerUtil}
import datax.constants.{ProductConstant, BlobProperties}
import datax.config.ConfigManager
import datax.host.SparkSessionSingleton
import org.apache.spark.broadcast
import org.apache.log4j.LogManager
import org.apache.spark.TaskContext
import org.apache.spark.rdd.RDD
import org.apache.spark.sql.{DataFrame, Row, SparkSession}

import scala.concurrent.duration.Duration

object BlobSinker extends SinkOperatorFactory {
  val SinkName = "Blobs"
  val DefaultOutputGroup = "main"

  def generateOutputFolderPath(folderFormat: String, outputTimestamp: Timestamp, target: Option[String]) = {
    if(folderFormat==null || folderFormat.isEmpty)
      null
    else {
      // val timestamp = new Timestamp(new java.util.Date().getTime)
      val minute = outputTimestamp.toLocalDateTime().getMinute()
      val quarter = Array("00", "15", "30", "45")
      val quarterBucket = quarter(Math.round(minute / 15))
      //getLogger.warn("Minute Bucket: " + quarterBucket)
      val simpleTimeFormat = new SimpleDateFormat("HHmmss")
      val minuteBucket = simpleTimeFormat.format(outputTimestamp)
      String.format(folderFormat, outputTimestamp)
        .replaceAllLiterally("${quarterBucket}", quarterBucket)
        .replaceAllLiterally("${minuteBucket}", minuteBucket)
        .replaceAllLiterally("${target}", target.getOrElse("UNKNOWN"))
        .stripSuffix("/") + "/"
    }
  }

  // write events to blob location
  def writeEventsToBlob(data: Seq[String], outputPath: String, compression: Boolean, blobStorageKey: broadcast.Broadcast[String] = null) {
    val logger = LogManager.getLogger(s"EventsToBlob-Writer${SparkEnvVariables.getLoggerSuffix()}")
    val countEvents = data.length

    val t1 = System.nanoTime()
    var timeLast = t1
    var timeNow: Long = 0
    logger.info(s"$timeNow:Partition started")

    //val data = it.toArray
    if (countEvents > 0) {
      timeNow = System.nanoTime()
      logger.info(s"$timeNow:Step 1: collected ${countEvents} records, spent time=${(timeNow - timeLast) / 1E9} seconds")
      timeLast = timeNow
      val content = if(compression){
        val result = GZipHelper.deflateToBytes(data)
        timeNow = System.nanoTime()
        logger.info(s"$timeNow:Step 2: compressed to ${result.length} bytes, spent time=${(timeNow - timeLast) / 1E9} seconds")
        timeLast = timeNow
        result
      }
      else {
        data.mkString("\n").getBytes
      }

      HadoopClient.writeWithTimeoutAndRetries(
        hdfsPath = outputPath,
        content = content,
        timeout = Duration.create(ConfigManager.getActiveDictionary().getOrElse(JobArgument.ConfName_BlobWriterTimeout, "10 seconds")),
        retries = 0,
        blobStorageKey
      )
      timeNow = System.nanoTime()
      logger.info(s"$timeNow:Step 3: done writing to $outputPath, spent time=${(timeNow - timeLast) / 1E9} seconds")
      timeLast = timeNow
    }

    logger.info(s"$timeNow:Done writing events ${countEvents} events, spent time=${(timeLast - t1) / 1E9} seconds")
  }

  def writeDatasetToBlobs(rdd: RDD[String], outputFolder: String, fileSuffix: String, compression: Boolean):RDD[ProcessResult] = {
    val outputPathPrefix = outputFolder.stripSuffix("/")
    rdd.mapPartitions(it=>{
      val tc = TaskContext.get()
      val logger = LogManager.getLogger(s"DatasetToBlobs-Writer${SparkEnvVariables.getLoggerSuffix()}")

      val t1 = System.nanoTime()
      var timeLast = t1
      var timeNow: Long = 0
      logger.info(s"$timeNow:Partition started")

      val dataAll = it.toArray
      val dataSize = dataAll.length

      timeNow = System.nanoTime()
      logger.info(s"$timeNow:Collected ${dataSize} events, spent time=${(timeNow - timeLast) / 1E9} seconds")
      timeLast = timeNow

      val path = outputPathPrefix + "/part-%05d".format(tc.partitionId()) + fileSuffix
      if(dataSize>0) {
        writeEventsToBlob(dataAll, path, compression)
        timeNow = System.nanoTime()
        logger.info(s"$timeNow:Done writting ${dataAll.length} events, spent time=${(timeNow - timeLast) / 1E9} seconds")
        timeLast = timeNow
        Iterator.single(ProcessResult(1, dataSize))
      }
      else {
        logger.warn(s"There is 0 events to output, skipped output partition file:'$path'")
        Iterator.single(ProcessResult(0, 0))
      }
    })
  }

  val MetricPrefixEvents = s"${MetricName.MetricSinkPrefix}${SinkName}_Events_"
  val MetricPrefixBlobs = s"${MetricName.MetricSinkPrefix}${SinkName}_Count_"

  def sinkDataGroups(rowInfo: Row,
                     dataGenerator: ()=>Map[String, Iterator[String]],
                     outputFolders: Map[String, String],
                     partitionId: Int,
                     compression: Boolean,
                     loggerSuffix: String,
                     blobStorageKey: broadcast.Broadcast[String]): Map[String, Int] = {
    val logger = LogManager.getLogger(s"Sinker-BlobSinker$loggerSuffix")
    val dataGroups = dataGenerator()
    val timeStart = System.nanoTime ()
    val eventCounts = dataGroups.flatMap {
      case (group, data) =>
        val fileName = FileInternal.getInfoOutputFileName(rowInfo)
        outputFolders.get(group) match {
          case None =>
            Seq(s"${MetricPrefixEvents}$group" -> 0, s"${MetricPrefixBlobs}$group" -> 0)
          case Some(folder) =>
            val path = folder +
              (if (fileName == null) s"part-$partitionId" else fileName) + (if(compression) ".json.gz" else ".json")
            val jsonList = data.toSeq
            BlobSinker.writeEventsToBlob(jsonList, path, compression, blobStorageKey )

            Seq(s"${MetricPrefixEvents}$group" -> jsonList.length, s"${MetricPrefixBlobs}$group" -> 1)
        }
    }

    val timeNow = System.nanoTime ()
    logger.info (s"$timeNow:Written all event groups ${eventCounts.toString}, spent time=${(timeNow - timeStart) / 1E9} seconds")
    eventCounts
  }

  def getRowsSinkerGenerator(blobOutputConf: BlobOutputConf, flagColumnIndex: Int) : SinkDelegate = {
    val compressionTypeConf = blobOutputConf.compressionType
    val formatConf = blobOutputConf.format
    if(formatConf.isDefined && !formatConf.get.equalsIgnoreCase("json"))
      throw new Error(s"Output format: ${formatConf.get} as specified in the config is not supported")
    val outputFolders = blobOutputConf.groups.map{case(k,v)=>k->KeyVaultClient.resolveSecretIfAny(v.folder)}

    val blobStorageKey = ExecutorHelper.createBlobStorageKeyBroadcastVariable(outputFolders.head._2, SparkSessionSingleton.getInstance(ConfigManager.initSparkConf))

    val jsonSinkDelegate = (rowInfo: Row, rows: Seq[Row], outputPartitionTime: Timestamp, partitionId: Int, loggerSuffix: String) => {
      val target = FileInternal.getInfoTargetTag(rowInfo)
      if(compressionTypeConf.isDefined && !(compressionTypeConf.get.equalsIgnoreCase("gzip")|| compressionTypeConf.get.equalsIgnoreCase("none")|| compressionTypeConf.get.equals("")))
        throw new Error(s"Output compressionType: ${compressionTypeConf.get} as specified in the config is not supported")
      val compression = compressionTypeConf.getOrElse("gzip").equalsIgnoreCase("gzip")

      sinkDataGroups(
        rowInfo = rowInfo,
        dataGenerator =
          if(flagColumnIndex<0)
            () => Map(DefaultOutputGroup -> rows.iterator.map(_.getString(1)))
          else
            () => rows.groupBy(_.getString(flagColumnIndex)).map { case (k, v) => k -> v.iterator.map(_.getString(1)) },
        outputFolders = outputFolders.map{case (k,v) =>
          k->generateOutputFolderPath(v, outputPartitionTime, Option(target))},
        partitionId = partitionId,
        compression = compression,
        loggerSuffix = loggerSuffix,
        blobStorageKey = blobStorageKey
      )
    }

    (data: DataFrame, time: Timestamp, loggerSuffix: String) => {
      SinkerUtil.sinkJson(data, time, jsonSinkDelegate)
    }
  }

  def getSinkOperator(dict: SettingDictionary, name: String): SinkOperator = {
    val blobConf = BlobOutputSetting.buildBlobOutputConf(dict, name)
    SinkOperator(
      name = SinkName,
      isEnabled = blobConf!=null,
      sinkAsJson = true,
      flagColumnExprGenerator = () =>  blobConf.groupEvaluation.getOrElse(null),
      generator = flagColumnIndex=>getRowsSinkerGenerator(blobConf, flagColumnIndex),
      onBatch = (spark: SparkSession, outputPartitionTime: Timestamp, targets: Set[String]) => {
        val logger = LogManager.getLogger(this.getClass)
        val groups = blobConf.groups
        val outputFolders = groups.filter(g=>g._2!=null && g._2.folder!=null)
          .flatMap(g=>{
            val actualFolder = KeyVaultClient.resolveSecretIfAny(g._2.folder)
            if(targets!=null && targets.size>0)
              targets.map(t=>generateOutputFolderPath(actualFolder, outputPartitionTime, Option(t)))
            else
              Seq(generateOutputFolderPath(actualFolder, outputPartitionTime, None))
          })
          .filter(_!=null)
          .toSet

        outputFolders.par.foreach(HadoopClient.createFolder)
        logger.warn(s"Created folders at ------\n${outputFolders.mkString("\n")}")
      }
    )
  }

  override def getSettingNamespace(): String = BlobOutputSetting.Namespace
}
