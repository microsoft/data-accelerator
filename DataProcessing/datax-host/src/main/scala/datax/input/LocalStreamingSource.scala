// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.input

import java.nio.charset.Charset

import com.microsoft.azure.eventhubs.EventData
import org.apache.log4j.LogManager
import org.apache.spark.storage._
import org.apache.spark.streaming.receiver._
import org.apache.spark.sql.types.{DataType, StructType}
import datax.utility


/** This is a test receiver that generates data. */
class LocalStreamingSource(inputSchema: DataType) extends Receiver[EventData](StorageLevel.MEMORY_AND_DISK_2) {

  /** Start the thread that receives data over a connection */
  def onStart() {
    new Thread("Local Data Source") { override def run() { receive() } }.start()
  }

  def onStop() {  }

  /** Periodically generate random data based on given schema */
  private def receive() {
    val logger = LogManager.getLogger("LocalStreamingSource")


    while(!isStopped()) {
      val jsonStr = DataGenerator.getRandomJson(inputSchema)
      logger.warn("Generated json="+jsonStr)
      val eventData = EventData.create(jsonStr.getBytes(Charset.defaultCharset()))
      store(Iterator(eventData))
      Thread.sleep(500)
    }
  }
}
