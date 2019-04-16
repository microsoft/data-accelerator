// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.processor

import java.sql.Timestamp

import com.microsoft.azure.eventhubs.EventData
import org.apache.spark.rdd.RDD

import scala.concurrent.duration.Duration

trait EventHubStreamingProcessor {
  val process: (RDD[EventData], Timestamp, Duration) => Map[String, Double]
}
