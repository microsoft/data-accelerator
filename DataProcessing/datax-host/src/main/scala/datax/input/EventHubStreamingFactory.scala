// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.input

import java.sql.Timestamp
import java.time.Instant

import com.microsoft.azure.eventhubs.EventData
import datax.checkpoint.EventhubCheckpointer
import datax.config.UnifiedConfig
import datax.constants.ProductConstant
import datax.exception.EngineException
import datax.input.EventHubInputSetting.InputEventHubConf
import datax.processor.EventHubStreamingProcessor
import datax.securedsetting.KeyVaultClient
import datax.telemetry.AppInsightLogger
import datax.utility.DateTimeUtil
import org.apache.log4j.LogManager
import org.apache.spark.eventhubs.{EventHubsConf, EventHubsUtils, EventPosition}
import org.apache.spark.eventhubs.rdd.HasOffsetRanges
import org.apache.spark.rdd.RDD
import org.apache.spark.streaming.{StreamingContext, Time}

object EventHubStreamingFactory {
  def getEventHubConf(eventhubInput:InputEventHubConf) = {
    val logger = LogManager.getLogger("EventHubConfBuilder")

    val connectionString = KeyVaultClient.resolveSecretIfAny(eventhubInput.connectionString)
    if(connectionString==null||connectionString.isEmpty){
      val errMsg = s"Connection string is empty for eventhub input"
      logger.error(errMsg)
      throw new EngineException(errMsg)
    }

    val checkpointDir = eventhubInput.checkpointDir
    val consumerGroup = eventhubInput.consumerGroup

    logger.warn("eventhub checkpointDir=" + checkpointDir)
    logger.warn("eventhub consumerGroup=" + consumerGroup)

    val ehConf = EventHubsConf(connectionString = connectionString)
      .setConsumerGroup(consumerGroup)
      .setMaxRatePerPartition(eventhubInput.maxRate.toInt)
      .setReceiverTimeout(java.time.Duration.ofSeconds(60))
      .setOperationTimeout(java.time.Duration.ofSeconds(120))

    eventhubInput.startEnqueueTime match {
      case Some(startEnqueueTimeInSeconds) =>
        if(startEnqueueTimeInSeconds<0){
          val startEnqueueTime = Instant.now.plusSeconds(startEnqueueTimeInSeconds)
          ehConf.setStartingPosition(EventPosition.fromEnqueuedTime(startEnqueueTime))
          logger.warn(s"eventhub startEnqueueTime from config:${startEnqueueTimeInSeconds}, passing startEnqueueTime=$startEnqueueTime")
        }
        else if(startEnqueueTimeInSeconds>0){
          val startEnqueueTime = Instant.ofEpochSecond(startEnqueueTimeInSeconds)
          ehConf.setStartingPosition(EventPosition.fromEnqueuedTime(startEnqueueTime))
          logger.warn(s"eventhub startEnqueueTime from config:${startEnqueueTimeInSeconds}, passing startEnqueueTime=$startEnqueueTime")
        }
        else{
          ehConf.setStartingPosition(EventPosition.fromStartOfStream)
        }
      case None =>
        ehConf.setStartingPosition(EventPosition.fromEndOfStream)
    }

    ehConf
  }

  def getStream(streamingContext: StreamingContext,
                              eventhubInput:InputEventHubConf,
                              foreachRDDHandler: (RDD[EventData], Time)=>Unit
                             ) ={
    ///////////////////////////////////////////////////////////////
    //Create direct stream from EventHub
    ///////////////////////////////////////////////////////////////
    val preparationLogger = LogManager.getLogger("PrepareEventHubDirectStream")
    val checkpointDir = eventhubInput.checkpointDir
    val ehConf = getEventHubConf(eventhubInput)
    if(eventhubInput.flushExistingCheckpoints.getOrElse(false))
      preparationLogger.warn("Flush the existing checkpoints according to configuration")
    else
      EventhubCheckpointer.applyCheckpointsIfExists(ehConf, checkpointDir)

    val checkpointIntervalInMilliseconds = eventhubInput.checkpointInterval.toLong*1000
    EventHubsUtils.createDirectStream(streamingContext, ehConf)
      //.persist()
      //.window(org.apache.spark.streaming.Duration.10))
      .foreachRDD((rdd, time)=>{
      AppInsightLogger.trackEvent(ProductConstant.ProductRoot + "/streaming/batch/begin", Map("batchTime"->time.toString), null)
      val offsetRanges = rdd.asInstanceOf[HasOffsetRanges].offsetRanges
      val batchTime = new Timestamp(time.milliseconds)
      val batchTimeStr = DateTimeUtil.formatSimple(batchTime)
      val streamLogger = LogManager.getLogger(s"CheckOffsets-${batchTimeStr}")

      streamLogger.warn(s"Processing offsets: \n" +
        offsetRanges.map(offset=>s"${offset.name}-${offset.partitionId.toString}: from=${offset.fromSeqNo}, until=${offset.untilSeqNo}").mkString("\n"))

      try {
        foreachRDDHandler(rdd, time)
      }
      catch {
        case e: Exception =>
          AppInsightLogger.trackException(e,
            Map("batchTime"->time.toString()),
            offsetRanges.map(offset=>s"${offset.name}-${offset.partitionId.toString}-fromSeqNo"->offset.fromSeqNo.toDouble).toMap)
          throw e
      }

      if(time.isMultipleOf(org.apache.spark.streaming.Duration(checkpointIntervalInMilliseconds))) {
        streamLogger.info(s"Start writing eventhub checkpoints to ${checkpointDir}")
        val conf = rdd.sparkContext.hadoopConfiguration
        EventhubCheckpointer.writeOffsetsToCheckpoints(checkpointDir, offsetRanges.map(r => (time.milliseconds, r.nameAndPartition.ehName, r.nameAndPartition.partitionId, r.fromSeqNo, r.untilSeqNo)), conf)
        streamLogger.warn(s"Done writing eventhub checkpoints to ${checkpointDir}")
      }

      AppInsightLogger.trackEvent(ProductConstant.ProductRoot + "/streaming/batch/end", Map("batchTime"->time.toString), null)
    })
  }

  @volatile private var instance: EventHubStreamingProcessor = null
  def getOrCreateProcessor(config: UnifiedConfig,
                                      generator: UnifiedConfig =>EventHubStreamingProcessor) = {
    if (instance == null) {
      synchronized {
        if (instance == null) {
          instance = generator(config)
        }
      }
    }

    instance
  }
}
