// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.input

import java.sql.Timestamp

import datax.constants.ProductConstant
import datax.telemetry.AppInsightLogger
import datax.utility.DateTimeUtil
import org.apache.kafka.clients.consumer.ConsumerRecord
import org.apache.kafka.common.serialization.StringDeserializer
import org.apache.log4j.LogManager
import org.apache.spark.rdd.RDD
import org.apache.spark.streaming.{StreamingContext, Time}
import org.apache.spark.streaming.kafka010.ConsumerStrategies.Subscribe
import org.apache.spark.streaming.kafka010.KafkaUtils
import org.apache.spark.streaming.kafka010.LocationStrategies.PreferConsistent


object KafkaStreamingFactory extends  StreamingFactory[ConsumerRecord[String,String]]{

  private def getKafkaConf(kafkaInput:InputKafkaConf) = {
    val logger = LogManager.getLogger("KafkaConfBuilder")

    val groupId = kafkaInput.groupId
    logger.warn("kafka group.id=" + groupId)

   var kafkaParams = Map[String, Object](
      "bootstrap.servers" -> kafkaInput.bootStrapServers,
      "key.deserializer" -> classOf[StringDeserializer],
      "value.deserializer" -> classOf[StringDeserializer],
      "group.id" -> groupId,
      "auto.offset.reset" -> kafkaInput.autoOffsetReset,
      "heartbeat.interval.ms" -> kafkaInput.heartBeatInterval,
      "session.timeout.ms" -> kafkaInput.sessionTimeout,
      "fetch.max.wait.ms" -> kafkaInput.fetchMaxWait
   )

    // EventHub for Kafka needs few additional params
    if(kafkaInput.isEventHubKafka) {
      kafkaParams += (
        "security.protocol" -> kafkaInput.securityProtocol,
        "sasl.mechanism" -> kafkaInput.saslMechanism,
        "sasl.jaas.config" -> kafkaInput.saslConfig
      )
    }

    //TODO: enable external store based checkpoint

    kafkaParams
  }

  def getStream(streamingContext: StreamingContext,
                inputConf:InputConf,
                foreachRDDHandler: (RDD[ConsumerRecord[String,String]], Time)=>Unit
               ) ={
    ///////////////////////////////////////////////////////////////
    //Create direct stream from Kafka
    ///////////////////////////////////////////////////////////////
    val preparationLogger = LogManager.getLogger("PrepareKafkaDirectStream")
    val kafkaInput =inputConf.asInstanceOf[InputKafkaConf]
    val kafkaParams = getKafkaConf(kafkaInput)
    if(kafkaInput.flushExistingCheckpoints.getOrElse(false))
      preparationLogger.warn("Flush the existing checkpoints according to configuration")

    val topics=kafkaInput.topics.split(",")
    preparationLogger.warn("Topics:" + topics.mkString(","))
    KafkaUtils.createDirectStream[String, String](
      streamingContext,
      PreferConsistent,
      Subscribe[String, String](topics, kafkaParams)
    ).foreachRDD((rdd, time)=>{
      AppInsightLogger.trackEvent(ProductConstant.ProductRoot + "/streaming/batch/begin", Map("batchTime"->time.toString), null)
      val batchTime = new Timestamp(time.milliseconds)
      val batchTimeStr = DateTimeUtil.formatSimple(batchTime)
      val streamLogger = LogManager.getLogger(s"CheckOffsets-${batchTimeStr}")

      try {
        foreachRDDHandler(rdd, time)
      }
      catch {
        case e: Exception =>
          AppInsightLogger.trackException(e)
          throw e
      }

      //todo: write checkpoints

      AppInsightLogger.trackEvent(ProductConstant.ProductRoot + "/streaming/batch/end", Map("batchTime"->time.toString), null)
    })
  }
}
