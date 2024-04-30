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
import org.apache.hadoop.fs.FileStatus
import org.apache.log4j.LogManager

import scala.language.postfixOps
import scala.collection.mutable.{HashSet, ListBuffer}
import scala.concurrent.duration._
import scala.util.Try

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
    val regex = """\{([yMdhHmsS\-='/.]+)\}*""".r

    regex.findFirstMatchIn(inputPath) match {
      case Some(partition) => partition.group(1)
      case None =>
        appLog.warn(s"InputPath string does not contain the datetime pattern ${regex.regex}.")
        ""
    }
  }

  def runBatchApp(inputArguments: Array[String],processorGenerator: UnifiedConfig=>BatchBlobProcessor ) = {
    val (appHost, config) = CommonAppHost.initApp(inputArguments, "Batch")

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

    val inputPartitionSizeThresholdInBytes = blobsConf.head.inputPartitionSizeThresholdInBytes
    appLog.warn(s"In runBatchApp, inputPartitionSizeThresholdInBytes: $inputPartitionSizeThresholdInBytes")
    val filterTimeRange = config.dict.getOrElse("filterTimeRange", "").equals("true")
    appLog.warn(s"filterTimeRange: $filterTimeRange")
    val filesToProcess = prefixes.flatMap(prefix=>HadoopClient.listFileObjects(prefix._1).flatMap(f=>inTimeRange(f, filterTimeRange)).toSeq)
    appLog.warn(s"filesToProcess: ${filesToProcess.length}")
    val minTimestamp = prefixes.minBy(_._2.getTime)._2
    appLog.warn(s"Start processing for $minTimestamp")
    val pathsRDD = sc.makeRDD(filesToProcess)
    val batchResult = processor.process(pathsRDD, minTimestamp, 1 hour, inputPartitionSizeThresholdInBytes)
    appLog.warn(s"End processing for $minTimestamp")

    appLog.warn(s"Batch Mode Work Ended, processed metrics: $batchResult")
    AppInsightLogger.trackEvent(ProductConstant.ProductRoot + "/batch/end", null, batchResult)

    //write tracker file
    val (trackerFolder, dateTimeStart, dateTimeEnd) = getTrackerConfigs(config)
    if (trackerFolder != "") {
      writeTracker(trackerFolder, dateTimeStart, dateTimeEnd)
    }
    /*
    This is a temporary workaround for https://github.com/Microsoft/ApplicationInsights-Java/issues/891
    */
    // This is a scala duration in string format, ie. 60 seconds (Default is zero)
    // Default value for this setting is to no wait if not specified
    val flushMetricDelay = config.dict.get("batchflushmetricsdelayduration")
      .flatMap(s => Try(Duration.create(s)).toOption)
      .getOrElse(Duration.Zero)
    val delayMillis = flushMetricDelay.toMillis
    if(delayMillis > 0) {
      appLog.warn(s"Waiting $delayMillis ms to allow metrics to be flushed completely")
      Thread.sleep(delayMillis) // Wait some time to allow flushing the app insight logger metrics
    }
  }

  // get tracker configs from input arguments and sys env
  // if the trackerFolder is configured, a tracker file should be written in the folder
  def getTrackerConfigs(config: UnifiedConfig) : (String, Timestamp, Timestamp) = {
    val inputRoot = config.dict.getOrElse("trackerFolder", "")
    val timeStart = sys.env.getOrElse("process_start_datetime", "")
    val timeEnd = sys.env.getOrElse("process_end_datetime", "")
    (inputRoot, Timestamp.from(Instant.parse(timeStart)), Timestamp.from(Instant.parse(timeEnd)))
  }

  // write a tracker file in specific folder with format _SUCCESS_yyyy_MM_dd_HH
  def writeTracker(trackerFolder: String, dt: Timestamp, dtEnd: Timestamp): Unit = {
    // first create folder if not exists
    HadoopClient.createFolder(trackerFolder)
    var fmt = "yyyy_MM_dd_HH"
    // if the interval is not 1h, the use the format yyyy_MM_dd_HH_mm
    if (dtEnd.getTime / 1000 - dt.getTime / 1000 + 1 != 3600) {
      fmt += "_mm"
    }
    val dateFormat = new SimpleDateFormat(fmt)

    val out = "_SUCCESS_" + dateFormat.format(dt)
    val outFilename = trackerFolder + out
    HadoopClient.writeHdfsFile(outFilename, "", overwriteIfExists=true, directWrite=true)
    appLog.warn(s"tracker file has been written: $outFilename")

  }

  def inTimeRange(fs: FileStatus, filterTimeRange: Boolean): Iterator[String] = {
    if (!filterTimeRange) {
      return Iterator(fs.getPath.toString)
    }
    val start = Timestamp.from(Instant.parse(sys.env.getOrElse("process_start_datetime", ""))).getTime / 1000
    val end = Timestamp.from(Instant.parse(sys.env.getOrElse("process_end_datetime", ""))).getTime / 1000
    val fTime = fs.getModificationTime / 1000
    appLog.warn(s"inTimeRange: start $start, end $end, fTime $fTime")

    if (start % 3600 == 0) {
      if (fTime <= end) Iterator(fs.getPath.toString) else Iterator.empty
    } else if ((end + 1) % 3600 == 0) {
      if (fTime >= start) Iterator(fs.getPath.toString) else Iterator.empty
    } else {
      if (fTime >= start && fTime <= end) Iterator(fs.getPath.toString) else Iterator.empty
    }
  }
}