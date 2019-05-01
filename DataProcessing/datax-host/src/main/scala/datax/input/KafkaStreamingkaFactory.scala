package datax.input

import java.sql.Timestamp

import com.microsoft.azure.eventhubs.EventData
import datax.constants.ProductConstant
import datax.exception.EngineException
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

object DStreamingKafkaFactory extends  StreamingFactory[ConsumerRecord[String,String]]{

  private def getKafkaConf(kafkaInput:InputKafkaConf) = {
    val logger = LogManager.getLogger("KafkaConfBuilder")

    val connectionString = "abc"//KeyVaultClient.resolveSecretIfAny(eventhubInput.connectionString)
    if(connectionString==null||connectionString.isEmpty){
      val errMsg = s"Connection string is empty for kafka input"
      logger.error(errMsg)
      throw new EngineException(errMsg)
    }

    val groupId = "$Default"//eventhubInput.groupId

    logger.warn("eventhub consumerGroup=" + groupId)

    val EH_SASL = "org.apache.kafka.common.security.plain.PlainLoginModule required username=\"$ConnectionString\" password=\"Endpoint=sb://inputext.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=aMwJX0anvp3z8N0oq2Qi/XOibaeU1NtcGXpOqddMe5A=\";"

    val kafkaParams = Map[String, Object](
      "bootstrap.servers" -> "inputext.servicebus.windows.net:9093",
      "key.deserializer" -> classOf[StringDeserializer],
      "value.deserializer" -> classOf[StringDeserializer],
      "group.id" -> "reader",
      "auto.offset.reset" -> "latest",
      "request.timeout.ms"->"90000",
      "heartbeat.interval.ms"->"20000",
      "session.timeout.ms"->"60000",
      "fetch.max.wait.ms"->"60000",
      "security.protocol"->"SASL_SSL",
      "sasl.mechanism"->"PLAIN",
      "sasl.jaas.config"->EH_SASL
    )

    //TODO
    /*eventhubInput.startEnqueueTime match {
      case Some(startEnqueueTimeInSeconds) =>
        if(startEnqueueTimeInSeconds<0){
          val startEnqueueTime = Instant.now.plusSeconds(startEnqueueTimeInSeconds)
          ehConf.setStartingPosition(EventPosition.fromEnqueuedTime(startEnqueueTime))
          logger.warn(s"eventhub startEnqueueTime from config:${startEnqueueTimeInSeconds}, passing startEnqueueTime=$startEnqueueTime")
        }
        else if(startEnqueueTimeInSeconds>0){
          val startEnqueueTime = Instant.ofEpochSecond(startEnqueueTimeInSeconds)
          ehConf.setStartingPosition(EventPosition.fromEnqueuedTime(startEnqueueTime))
          logger.warn(s"eventhub startEnqueueTime from config:${startEnqueueTimeInSeconds}, passing startEnqueueTime=$startEnqueueTime")
        }
        else{
          ehConf.setStartingPosition(EventPosition.fromStartOfStream)
        }
      case None =>
        ehConf.setStartingPosition(EventPosition.fromEndOfStream)
    }*/

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
    val eventhubInput =inputConf.asInstanceOf[InputKafkaConf]
    val kafkaParams = getKafkaConf(eventhubInput)
    if(eventhubInput.flushExistingCheckpoints.getOrElse(false))
      preparationLogger.warn("Flush the existing checkpoints according to configuration")

    val topics=Array("kafka1")

    KafkaUtils.createDirectStream[String, String](
      streamingContext,
      PreferConsistent,
      Subscribe[String, String](topics, kafkaParams)
    ).foreachRDD((rdd, time)=>{
      AppInsightLogger.trackEvent(ProductConstant.ProductRoot + "/streaming/batch/begin", Map("batchTime"->time.toString), null)
      // val offsetRanges = rdd.asInstanceOf[HasOffsetRanges].offsetRanges
      val batchTime = new Timestamp(time.milliseconds)
      val batchTimeStr = DateTimeUtil.formatSimple(batchTime)
      val streamLogger = LogManager.getLogger(s"CheckOffsets-${batchTimeStr}")

      //// streamLogger.warn(s"Processing offsets: \n" +
      //  offsetRanges.map(offset=>s"${offset.name}-${offset.partitionId.toString}: from=${offset.fromSeqNo}, until=${offset.untilSeqNo}").mkString("\n"))

      try {
        foreachRDDHandler(rdd, time)
      }
      catch {
        case e: Exception =>
          AppInsightLogger.trackException(e)//,
          // Map("batchTime"->time.toString())
          //  offsetRanges.map(offset=>s"${offset.name}-${offset.partitionId.toString}-fromSeqNo"->offset.fromSeqNo.toDouble).toMap)
          throw e
      }

      //todo: write checkpoints

      AppInsightLogger.trackEvent(ProductConstant.ProductRoot + "/streaming/batch/end", Map("batchTime"->time.toString), null)
    })
  }

}
