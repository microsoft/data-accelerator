// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.client.eventhub

import com.microsoft.azure.eventhubs.{EventData, EventHubClient}
import datax.exception.EngineException
import org.apache.log4j.{LogManager, Logger}

import scala.collection.JavaConverters._

class EventHubSender(client: EventHubClient){
  val senderName : String = this.getClass.getSimpleName
  def getLogger(): Logger = LogManager.getLogger(this.getClass)

  def sendString(data: String, properties: Map[String, String]) ={
    if(data!=null) {
      sendBytes(data.getBytes, properties)
    }
  }

  def sendBytes(data: Array[Byte], properties: Map[String, String]) = {
    val eventData = EventData.create(data)
    if(properties!=null)
      eventData.getProperties().putAll(properties.asJava)
    val t1=System.nanoTime()
    client.send(eventData)
    val et = (System.nanoTime()-t1)/1E9
    getLogger().info(s"sent ${data.length} bytes in $et seconds")
  }

  def sendAvroData(data: Array[Byte] ) ={
    if(data!=null) {
      val eventData = EventData.create(data)
      client.send(eventData)
      getLogger.info("avro eventData length = " + data.length)
    }
  }

  def close() = {
    if(client!=null){
      client.closeSync()
    }
  }
}

object EventHubSenderPool {
  private var sender:EventHubSender = null
  def getSender(outputEventHubConf: EventHubConf): EventHubSender ={
    if(outputEventHubConf == null
      || outputEventHubConf.connectionString==null
      || outputEventHubConf.connectionString.isEmpty){
      throw new EngineException(s"Unexpected empty eventhub conf")
    }

    if(sender==null){
      this.synchronized {
        if (sender == null) {
          LogManager.getLogger(this.getClass).warn(s"Constructing eventhub sender for ${outputEventHubConf.name}")
          sender = new EventHubSender(EventHubBase.getOutputClient(outputEventHubConf.connectionString))
        }
      }
    }

    sender
  }

}
