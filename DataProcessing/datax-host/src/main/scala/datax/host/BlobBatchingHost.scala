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
import datax.exception.EngineException
import datax.fs.HadoopClient
import datax.input.BatchBlobInputSetting
import datax.processor.{BatchBlobProcessor, BlobPointerProcessor}
import datax.telemetry.AppInsightLogger
import datax.utility.DataMerger
import org.apache.log4j.LogManager

import scala.language.postfixOps
import scala.collection.mutable.{HashSet, ListBuffer}
import scala.collection.parallel.ExecutionContextTaskSupport
import scala.concurrent.ExecutionContext
import scala.concurrent.duration._

object BlobBatchingHost {
  val appLog = LogManager.getLogger("runBatchApp")

  private def getInputBlobPathPrefixes(path: String, startTime: Instant):Iterable[(String, Timestamp)] = {
    val result = new ListBuffer[(String, Timestamp)]

    val pattern = getDateTimePattern(path)

    if(!pattern.isEmpty) {
      val dateFormat = new SimpleDateFormat(pattern)
      val timestamp = Timestamp.from(startTime)
      val partitionFolder = dateFormat.format(timestamp)
      val path2 = path.replace("{" + pattern + "}", partitionFolder)
      result += Tuple2(path2, timestamp)
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

      getInputBlobPathPrefixes(
        path = inputBlobPath,
        startTime = inputBlobStartTime
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
      val result = processor.process(pathsRDD, prefix._2, 1 hour)
      appLog.warn(s"End processing ${prefix}")

      result
    }).reduce(DataMerger.mergeMapOfDoubles)

    appLog.warn(s"Batch Mode Work Ended, processed metrics: $batchResult")
    AppInsightLogger.trackEvent(ProductConstant.ProductRoot + "/batch/end", null, batchResult)
  }

}
