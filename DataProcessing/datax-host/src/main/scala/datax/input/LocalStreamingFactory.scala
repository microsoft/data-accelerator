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
import org.apache.spark.sql.types.{DataType, StructType}
import org.apache.spark.streaming.{StreamingContext, Time}

// Factory class for streaming local events
object LocalStreamingFactory {

  def getStream(streamingContext: StreamingContext,
                inputSchema: DataType,
                           foreachRDDHandler: (RDD[EventData], Time)=>Unit
                          ) ={

    val preparationLogger = LogManager.getLogger("PrepareLocalDirectStream")
    ///////////////////////////////////////////////////////////////
    //Create direct stream from custom receiver
    ///////////////////////////////////////////////////////////////
    streamingContext.receiverStream(new LocalStreamingSource(inputSchema))
      .foreachRDD((rdd, time)=>{
        AppInsightLogger.trackEvent(ProductConstant.ProductRoot + "/localstreaming/batch/begin", Map("batchTime"->time.toString), null)
        val batchTime = new Timestamp(time.milliseconds)
        val batchTimeStr = DateTimeUtil.formatSimple(batchTime)
        val streamLogger = LogManager.getLogger(s"CheckOffsets-${batchTimeStr}")

        try {
          foreachRDDHandler(rdd, time)
        }
        catch {
          case e: Exception =>
            AppInsightLogger.trackException(e,
              Map("batchTime"->time.toString()),
              Map("batchMetric"->1))
            throw e
        }

        AppInsightLogger.trackEvent(ProductConstant.ProductRoot + "/localstreaming/batch/end", Map("batchTime"->time.toString), null)
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
