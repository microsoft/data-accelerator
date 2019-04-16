// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.processor

import java.sql.Timestamp

import com.microsoft.azure.eventhubs.EventData
import org.apache.spark.rdd.RDD
import org.apache.spark.sql.DataFrame
import org.apache.spark.sql.streaming.StreamingQuery

import scala.concurrent.duration.Duration

case class CommonProcessor( processJson: (RDD[String], Timestamp, Duration, Timestamp) => Map[String, Double],
                            processEventHubDataFrame: (DataFrame) => Map[String, StreamingQuery],
                            processEventData: (RDD[EventData], Timestamp, Duration, Timestamp) => Map[String, Double],
                            processPaths: (RDD[String], Timestamp, Duration, Timestamp, String) => Map[String, Double]){

  def asBlobPointerProcessor() = new BlobPointerProcessor(processPaths = this.processPaths)
  def asJsonProcessor() = new JsonProcessor(processJson = this.processJson)
  def asDirectProcessor() = new DirectProcessor(processEventData = this.processEventData)
  def asStructuredStreamingProcessor = new EventHubStructuredStreamingProcessor(processDataFrame = this.processEventHubDataFrame)
  def asDirectLocalProcessor() = new DirectLocalProcessor(processEventData = this.processEventData)
}
