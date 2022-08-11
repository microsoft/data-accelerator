// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.input

import java.sql.Timestamp
import com.microsoft.azure.eventhubs.EventData
import datax.constants.ProductConstant
import datax.telemetry.{AppInsightLogger, ReportedException}
import datax.utility.DateTimeUtil
import org.apache.log4j.LogManager
import org.apache.spark.rdd.RDD
import org.apache.spark.streaming.{StreamingContext, Time}

// Factory class for streaming local events
object LocalStreamingFactory extends  StreamingFactory[EventData]{

  def getStream(streamingContext: StreamingContext,
                inputConf:InputConf,
                           foreachRDDHandler: (RDD[EventData], Time)=>Unit
                          ) ={

    val preparationLogger = LogManager.getLogger("PrepareLocalDirectStream")

    val localInput =inputConf.asInstanceOf[InputLocalConf]
    ///////////////////////////////////////////////////////////////
    //Create direct stream from custom receiver
    ///////////////////////////////////////////////////////////////
    streamingContext.receiverStream(new LocalStreamingSource(localInput.inputSchema))
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
            throw ReportedException(e)
        }

        AppInsightLogger.trackEvent(ProductConstant.ProductRoot + "/localstreaming/batch/end", Map("batchTime"->time.toString), null)
      })
  }

}
