// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

package datax.sink

import datax.utility.{GZipHelper, SinkerUtil}
import datax.client.eventhub.{EventHubConf, EventHubSenderPool}
import datax.config.SettingDictionary
import datax.exception.EngineException
import datax.sink.EventHubOutputSetting.EventHubOutputConf
import org.apache.log4j.LogManager

object EventHubStreamPoster extends SinkOperatorFactory {
  val SinkName = "EventHub"
  def sendFilteredEvents(data: Seq[String],
                         outputEventhubConf: EventHubConf,
                         appendProperties: Map[String, String],
                         loggerSuffix: String,
                         compressionType: String): Int = {

    val logger = LogManager.getLogger(s"FilteredEvent-Sender${loggerSuffix}")
    val countEvents = data.length
    val chunkSize = 200
    if (countEvents > 0) {
      val sender = EventHubSenderPool.getSender(outputEventhubConf)
      var i = 0
      data.grouped(chunkSize).foreach(events => {
        val eventsSize = events.length
        val json = events.mkString("\n")
        val t1 = System.nanoTime()
        val elpasedTime = (System.nanoTime() - t1) / 1E9
        val stage = s"[$i-${i + eventsSize}]/$countEvents"
        if(compressionType.equalsIgnoreCase(EventHubOutputSetting.CompressionValueGZip)) {
          val compressedJson = GZipHelper.deflateToBytes(json)
          logger.info(s"$stage: compressed filtered events, count=$eventsSize, json=${json.length} bytes, compressed= ${compressedJson.length} bytes, spent time=$elpasedTime seconds")
          sender.sendBytes(compressedJson, appendProperties)
        }
        else
        {
          logger.info(s"$stage: compressed filtered events, count=$eventsSize, json=${json.length} bytes, spent time=$elpasedTime seconds")
          sender.sendBytes(json.getBytes(), appendProperties)
        }
        logger.info(s"$stage: done sending")
        i += eventsSize
        eventsSize
      })
      countEvents
    }
    else 0
  }

  def getRowsSinkerGenerator(conf: EventHubOutputConf, flagColumnIndex: Int) : SinkDelegate = {
    val format = conf.format
    if(!format.equalsIgnoreCase(EventHubOutputSetting.FormatValueJson))
      throw new EngineException(s"Eventhub: Output format: ${format} as specified in the config is not supported")

    val compressionType = conf.compressionType
    if(compressionType!=EventHubOutputSetting.CompressionValueNone && compressionType!=EventHubOutputSetting.CompressionValueGZip)
      throw new EngineException(s"EventHub: compressionType: ${compressionType} as specified in the config is not supported")

    val sender = (dataToSend: Seq[String], ls: String) => EventHubStreamPoster.sendFilteredEvents(dataToSend, EventHubConf(
      name = SinkerUtil.hashName(conf.connectionString),
      connectionString = conf.connectionString
    ), conf.appendProperties, ls, compressionType)
    SinkerUtil.outputGenerator(sender,SinkName)(flagColumnIndex)
  }

  def getSinkOperator(dict: SettingDictionary, name: String) : SinkOperator = {
    val conf = EventHubOutputSetting.buildEventHubOutputConf(dict, name)
    SinkOperator(
      name = SinkName,
      isEnabled = conf!=null,
      sinkAsJson = true,
      flagColumnExprGenerator = () => conf.filter,
      generator = flagColumnIndex => getRowsSinkerGenerator(conf, flagColumnIndex)
    )
  }

  override def getSettingNamespace(): String = EventHubOutputSetting.Namespace
}
