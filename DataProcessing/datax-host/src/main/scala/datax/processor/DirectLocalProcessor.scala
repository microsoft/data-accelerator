// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.processor

import java.sql.Timestamp
import com.microsoft.azure.eventhubs.EventData
import datax.telemetry.AppInsightLogger
import datax.utility.DateTimeUtil
import org.apache.spark.rdd.RDD

import scala.concurrent.duration.Duration

// Data processor for processing events in OneBox mode where job running is running locally
class DirectLocalProcessor(processEventData: (RDD[EventData], Timestamp, Duration, Timestamp) => Map[String, Double])
  extends StreamingProcessor[EventData]{
  override val process = (rdd: RDD[EventData], batchTime: Timestamp, batchInterval: Duration) => {
    val outputPartitionTime =DateTimeUtil.getCurrentTime()
    AppInsightLogger.InstrumentedFunction((Unit) => processEventData(rdd, batchTime, batchInterval, outputPartitionTime), "UncaughtException")
  }
}
