// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.host

import java.sql.Timestamp
import java.text.SimpleDateFormat
import java.time.Instant
import java.util.concurrent.Executors

import datax.config.UnifiedConfig
import datax.constants.ProductConstant
import datax.fs.HadoopClient
import datax.input.BatchBlobInputSetting
import datax.processor.BlobPointerProcessor
import datax.telemetry.AppInsightLogger
import datax.utility.DataMerger
import org.apache.log4j.LogManager

import scala.language.postfixOps
import scala.collection.mutable.{HashSet, ListBuffer}
import scala.collection.parallel.ExecutionContextTaskSupport
import scala.concurrent.ExecutionContext
import scala.concurrent.duration._

object BlobBatchingHost {
  def getInputBlobPathPrefixes(prefix: String, datetimeFormat: String, startTime: Instant, durationInSeconds: Long, timeIntervalInSeconds: Long):Iterable[(String, Timestamp)] = {
    val result = new ListBuffer[(String, Timestamp)]
    val cache = new HashSet[String]

    var t:Long = 0
    //val utcZoneId = ZoneId.of("UTC")
    val dateFormat = new SimpleDateFormat(datetimeFormat)
    while(t<durationInSeconds){
      val timestamp = Timestamp.from(startTime.plusSeconds(t))
      val partitionFolder = dateFormat.format(timestamp)
      if(!cache.contains(partitionFolder)){
        val path = prefix+partitionFolder
        result += Tuple2(path, timestamp)
        cache += partitionFolder
      }
      t+= timeIntervalInSeconds
    }

    result
  }

  def runBatchApp(inputArguments: Array[String],processorGenerator: UnifiedConfig=>BlobPointerProcessor ) = {
    val appLog = LogManager.getLogger("runBatchApp")
    val (appHost, config) = CommonAppHost.initApp(inputArguments)

    appLog.warn(s"Batch Mode Work Started")

    val blobsConf = BatchBlobInputSetting.getInputBlobsArrayConf(config.dict)
    AppInsightLogger.trackEvent(ProductConstant.ProductRoot + "/batch/app/begin")

    val prefixes = blobsConf.flatMap(blobs=>{
      val inputBlobPathPrefix = blobs.pathPrefix
      val inputBlobDateTimeFormat = blobs.pathPartitionFolderFormat
      val inputBlobStartTime = Instant.parse(blobs.startTime)
      val inputBlobDurationInHours = blobs.durationInHours
      val inputBlobTimeIntervalInHours = 1

      getInputBlobPathPrefixes(
        prefix = inputBlobPathPrefix,
        datetimeFormat = inputBlobDateTimeFormat,
        startTime = inputBlobStartTime,
        durationInSeconds = inputBlobDurationInHours*3600,
        timeIntervalInSeconds = inputBlobTimeIntervalInHours*3600
      )
    }).par

    val spark = appHost.getSpark(config.sparkConf)
    val sc = spark.sparkContext
    val processor = processorGenerator(config)

    val ec = new ExecutionContext {
      val threadPool = Executors.newFixedThreadPool(16)
      def execute(runnable: Runnable) {
        threadPool.submit(runnable)
      }
      def reportFailure(t: Throwable) {}
    }

    prefixes.tasksupport = new ExecutionContextTaskSupport(ec)

    val batchResult = prefixes.map(prefix =>{
      appLog.warn(s"Start processing ${prefix}")
      val namespace = "_"+HadoopClient.tempFilePrefix(prefix._1)
      appLog.warn(s"Namespace for prefix ${prefix._1} is '$namespace'")
      val pathsRDD = sc.makeRDD(HadoopClient.listFiles(prefix._1).toSeq)
      val result = processor.processPathsRDD(pathsRDD, prefix._2, 1 hour, prefix._2, namespace)
      appLog.warn(s"End processing ${prefix}")

      result
    }).reduce(DataMerger.mergeMapOfDoubles)

    appLog.warn(s"Batch Mode Work Ended, processed metrics: $batchResult")
    AppInsightLogger.trackEvent(ProductConstant.ProductRoot + "/batch/end", null, batchResult)
  }

}
