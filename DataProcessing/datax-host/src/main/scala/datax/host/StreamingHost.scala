// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.host

import java.sql.Timestamp

import com.microsoft.azure.eventhubs.EventData
import datax.config._
import datax.constants.{JobArgument, ProductConstant}
import datax.exception.EngineException
import datax.input._
import datax.processor.EventHubStreamingProcessor
import datax.telemetry.AppInsightLogger
import datax.utility.DateTimeUtil
import org.apache.log4j.LogManager
import org.apache.spark.rdd.RDD
import org.apache.spark.sql.SparkSession
import org.apache.spark.streaming.{Seconds, StreamingContext}

import scala.concurrent.duration._

object StreamingHost {
  def getLogger = LogManager.getLogger(this.getClass)

  def createStreamingContext(spark: SparkSession, intervalInSeconds: Long) = {
    new StreamingContext(spark.sparkContext, Seconds(intervalInSeconds))
  }

  def createStreamingContextWithCheckpoint(spark:SparkSession, streamingCheckpointDir: String, intervalInSeconds: Long) = {
    val streamingContext = createStreamingContext(spark, intervalInSeconds)
    getLogger.warn("create a new streaming context with checkpointDir=" + streamingCheckpointDir)
    streamingContext.checkpoint(streamingCheckpointDir)
    streamingContext
  }

  def initStreamingContext(spark: SparkSession, streamingCheckpointDir: String, intervalInSeconds: Long) = {
    getLogger.warn("spark streaming checkpointDir=" + streamingCheckpointDir)
    StreamingContext.getOrCreate(streamingCheckpointDir,
      () => createStreamingContextWithCheckpoint(spark, streamingCheckpointDir, intervalInSeconds),
      spark.sparkContext.hadoopConfiguration,
      false
    )
  }

  def runStreamingApp(inputArguments: Array[String], processorGenerator: UnifiedConfig=>EventHubStreamingProcessor): Unit = {
    val (appHost, config) = CommonAppHost.initApp(inputArguments)
    val spark = CommonAppHost.getSpark(config.sparkConf)

    val dict = config.dict
    val streamingConf = StreamingInputSetting.getStreamingInputConf(dict)
    val eventhubConf = EventHubInputSetting.getInputEventHubConf(dict)
    if(eventhubConf==null)
      throw new EngineException(s"No proper eventhub config is provided")

    val logger = LogManager.getLogger("runStreamingApp")
    logger.warn(s"Get or create streaming context from checkpoint folder:${streamingConf.checkpointDir}")
    val checkpointEnabled = dict.getOrElse(JobArgument.ConfName_CheckpointEnabled, "false").toBoolean

    def createSC() = {
      val createStreamContextLogger = LogManager.getLogger("runStreamingApp-createSC")
      val spark = CommonAppHost.getSpark(config.sparkConf)
      createStreamContextLogger.warn(s"Create streaming context checkpoints folder=${streamingConf.checkpointDir}, internalInSeconds=${streamingConf.intervalInSeconds}")
      val streamingContext = createStreamingContext(spark, streamingConf.intervalInSeconds)
      val batchInterval = streamingConf.intervalInSeconds.seconds
      val repartitionNumber = eventhubConf.repartition.getOrElse(0)
      val repartition = if(repartitionNumber==0) (r:RDD[EventData])=>r else (r:RDD[EventData])=>r.repartition(repartitionNumber)
      EventHubStreamingFactory.getStream(streamingContext, eventhubConf, (rdd, time) => {
        val streamingLogger = LogManager.getLogger("EventHubStreamingLoop")
        val batchTime = new Timestamp(time.milliseconds)
        val batchTimeStr = DateTimeUtil.formatSimple(batchTime)
        streamingLogger.warn(s"===============================Batch $batchTimeStr Started===============================")
        val processor = EventHubStreamingFactory.getOrCreateProcessor(config, processorGenerator)
        processor.process(repartition(rdd), batchTime, batchInterval)
        streamingLogger.warn(s"===============================Batch $batchTimeStr End    ===============================")
      })

      streamingContext
    }

    val streamingContext = if(checkpointEnabled)
      StreamingContext.getOrCreate(
        streamingConf.checkpointDir,
        createSC _,
        spark.sparkContext.hadoopConfiguration,
        false)
    else createSC()

    //streamingContext.remember(org.apache.spark.streaming.Duration(65000))
    streamingContext.start()

    AppInsightLogger.trackEvent(ProductConstant.ProductRoot + "/streaming/app/start")
    logger.warn(s"Streaming Context Started")
    streamingContext.awaitTermination()
  }

  def runLocalStreamingApp(inputArguments: Array[String], processorGenerator: UnifiedConfig=>EventHubStreamingProcessor): Unit = {
    val (appHost, config) = CommonAppHost.initApp(inputArguments)
    val spark = CommonAppHost.getSpark(config.sparkConf)

    val dict = config.dict
    val streamingConf = StreamingInputSetting.getStreamingInputConf(dict)
    val logger = LogManager.getLogger("runLocalStreamingApp")
    logger.warn(s"Get or create streaming context from checkpoint folder:${streamingConf.checkpointDir}")
    val checkpointEnabled = dict.getOrElse(JobArgument.ConfName_CheckpointEnabled, "false").toBoolean

    def createSC() = {
      val createStreamContextLogger = LogManager.getLogger("runLocalStreamingApp-createSC")
      val spark = CommonAppHost.getSpark(config.sparkConf)
      createStreamContextLogger.warn(s"Create streaming context checkpoints folder=${streamingConf.checkpointDir}, internalInSeconds=${streamingConf.intervalInSeconds}")
      val streamingContext = createStreamingContext(spark, streamingConf.intervalInSeconds)
      val batchInterval = streamingConf.intervalInSeconds.seconds

      val inputSchema = SchemaFile.loadInputSchema(dict)
      LocalStreamingFactory.getStream(streamingContext, inputSchema, (rdd, time) => {
        val streamingLogger = LogManager.getLogger("LocalStreamingLoop")
        val batchTime = new Timestamp(time.milliseconds)
        val batchTimeStr = DateTimeUtil.formatSimple(batchTime)
        streamingLogger.warn(s"===============================Batch $batchTimeStr Started===============================")
        val processor = LocalStreamingFactory.getOrCreateProcessor(config, processorGenerator)
        processor.process(rdd, batchTime, batchInterval)
        streamingLogger.warn(s"===============================Batch $batchTimeStr End    ===============================")
      })

      streamingContext
    }

    val streamingContext = if(checkpointEnabled)
      StreamingContext.getOrCreate(
        streamingConf.checkpointDir,
        createSC _,
        spark.sparkContext.hadoopConfiguration,
        false)
    else createSC()

    streamingContext.start()

    AppInsightLogger.trackEvent(ProductConstant.ProductRoot + "/localStreaming/app/start")
    logger.warn(s"Local Streaming Context Started")
    streamingContext.awaitTermination()
  }
}
