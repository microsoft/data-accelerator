// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.processor

import datax.telemetry.AppInsightLogger

import java.sql.Timestamp
import datax.utility.DateTimeUtil
import org.apache.kafka.clients.consumer.ConsumerRecord
import org.apache.spark.rdd.RDD

import scala.concurrent.duration.Duration

// Class to invoke ConsumerRecords processor from Kafka input
class DirectKafkaProcessor (processConsumerRecord: (RDD[ConsumerRecord[String,String]], Timestamp, Duration, Timestamp) => Map[String, Double])
  extends StreamingProcessor[ConsumerRecord[String,String]] {
  override val process = (rdd: RDD[ConsumerRecord[String,String]], batchTime: Timestamp, batchInterval: Duration) => {
    val outputPartitionTime = DateTimeUtil.getCurrentTime()
    AppInsightLogger.InstrumentedFunction((Unit) => processConsumerRecord(rdd, batchTime, batchInterval, outputPartitionTime), "UncaughtException")
  }
}
