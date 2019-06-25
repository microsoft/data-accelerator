// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.host

import java.sql.Timestamp
import java.text.SimpleDateFormat
import java.time.Instant
import java.time.temporal.ChronoUnit

import datax.config.UnifiedConfig
import datax.constants.ProductConstant
import datax.data.FileInternal
import datax.fs.HadoopClient
import datax.input.BatchBlobInputSetting
import datax.processor.{BatchBlobProcessor, CommonProcessorFactory}
import datax.telemetry.AppInsightLogger
import org.apache.log4j.LogManager

import scala.language.postfixOps
import scala.collection.mutable.{HashSet, ListBuffer}
import scala.concurrent.duration._

object BlobBatchingHost {
  val appLog = LogManager.getLogger("runBatchApp")

 def getInputBlobPathPrefixes(path: String, startTime: Instant, processingWindowInSeconds: Long, partitionIncrementDurationInSeconds: Long):Iterable[(String, Timestamp)] = {
    val result = new ListBuffer[(String, Timestamp)]
    val cache = new HashSet[String]

    val datetimeFormat = getDateTimePattern(path)
    var t:Long = 0

    if(!datetimeFormat.isEmpty) {
      val dateFormat = new SimpleDateFormat(datetimeFormat)
      while (t <= processingWindowInSeconds) {
        val timestamp = Timestamp.from(startTime.plusSeconds(t))
        val partitionFolder = dateFormat.format(timestamp)
        val path2 = path.replace("{" + datetimeFormat + "}", partitionFolder)
        if (!cache.contains(partitionFolder)) {
          result += Tuple2(path2, timestamp)
          cache += partitionFolder
        }
        t += partitionIncrementDurationInSeconds
      }
    }
    else {
      result += Tuple2(path, Timestamp.from(Instant.now()))
    }

    result
  }

  // Get the datetime pattern like {yyyy-MM-dd} in the input path.
  // For e.g. if the input path is "wasbs://outputs@myaccount.blob.core.windows.net/{yyyy-MM-dd}/flow1", this will return yyyy-MM-dd
  private def getDateTimePattern(inputPath:String):String ={
    val regex = """\{([yMdHmsS\-/.]+)\}*""".r

    regex.findFirstMatchIn(inputPath) match {
      case Some(partition) => partition.group(1)
      case None =>
        appLog.warn(s"InputPath string does not contain the datetime pattern ${regex.regex}.")
        ""
    }
  }

  def runBatchApp(inputArguments: Array[String],processorGenerator: UnifiedConfig=>BatchBlobProcessor ) = {
    val (appHost, config) = CommonAppHost.initApp(inputArguments)

    appLog.warn(s"Batch Mode Work Started")

    val blobsConf = BatchBlobInputSetting.getInputBlobsArrayConf(config.dict)
    AppInsightLogger.trackEvent(ProductConstant.ProductRoot + "/batch/app/begin")

    val prefixes = blobsConf.flatMap(blobs=>{
      val inputBlobPath= blobs.path
      val inputBlobStartTime = Instant.parse(blobs.startTime)
      val inputBlobEndTime = Instant.parse(blobs.endTime)

      val inputBlobProcessingWindowInSec= inputBlobStartTime.until(inputBlobEndTime, ChronoUnit.SECONDS)
      val inputBlobPartitionIncrementInSec = blobs.partitionIncrementInMin*60

      getInputBlobPathPrefixes(
        path = inputBlobPath,
        startTime = inputBlobStartTime,
        inputBlobProcessingWindowInSec,
        inputBlobPartitionIncrementInSec
      )
    })

    val spark = appHost.getSpark(config.sparkConf)
    val sc = spark.sparkContext
    val processor = processorGenerator(config)

    val filesToProcess = prefixes.flatMap(prefix=>HadoopClient.listFiles(prefix._1).toSeq)
    val minTimestamp = prefixes.minBy(_._2.getTime)._2
    appLog.warn(s"Start processing for $minTimestamp")
    val pathsRDD = sc.makeRDD(filesToProcess)
    val batchResult = processor.process(pathsRDD, minTimestamp, 1 hour)
    appLog.warn(s"End processing for $minTimestamp")

    appLog.warn(s"Batch Mode Work Ended, processed metrics: $batchResult")
    AppInsightLogger.trackEvent(ProductConstant.ProductRoot + "/batch/end", null, batchResult)
  }
}