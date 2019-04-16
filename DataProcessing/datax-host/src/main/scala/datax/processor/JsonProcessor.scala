// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.processor

import java.sql.Timestamp

import com.microsoft.azure.eventhubs.EventData
import datax.utility.DateTimeUtil
import org.apache.spark.rdd.RDD

import scala.concurrent.duration.Duration

class JsonProcessor(processJson: (RDD[String], Timestamp, Duration, Timestamp) => Map[String, Double])
  extends EventHubStreamingProcessor{
  override val process = (rdd: RDD[EventData], batchTime: Timestamp, batchInterval: Duration) => {
    val outputPartitionTime = DateTimeUtil.getCurrentTime()
    processJson(rdd.map(w=>new String(w.getBytes)), batchTime, batchInterval, outputPartitionTime)
  }
}
