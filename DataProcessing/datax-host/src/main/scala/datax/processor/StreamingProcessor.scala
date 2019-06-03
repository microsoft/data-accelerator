// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.processor

import java.sql.Timestamp
import org.apache.spark.rdd.RDD

import scala.concurrent.duration.Duration

// Interface for DStream processor
trait StreamingProcessor[T] {
  val process: (RDD[T], Timestamp, Duration) => Map[String, Double]
}
