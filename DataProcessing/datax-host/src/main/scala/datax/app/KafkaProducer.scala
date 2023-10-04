// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.app

import org.apache.kafka.clients.producer._

import java.util.Properties
import datax.fs.HadoopClient
import datax.host.SparkSessionSingleton
import datax.input.DataGenerator
import datax.securedsetting.KeyVaultClient
import org.apache.log4j.LogManager
import org.apache.spark.SparkConf
import org.apache.spark.sql.types.DataType

// Utility object which produces simulated data to kafka based in input schema
object KafkaProducer {
  def main(args: Array[String]): Unit = {

    val logger = LogManager.getLogger("KafkaProducer")

    if (args.length < 3){
      logger.warn("Usage: mvn exec:java -Dexec.mainClass=\"datax.app.KafkaProducer\" <schemaFile> <comma separated topicNames> <comma separated brokerhosts>")
      return
    }

    // Init spark to make yarn happy
    val sparkConf = new SparkConf()
    val spark = SparkSessionSingleton.getInstance(sparkConf)

    // Get input args
    val schemaFile = args(0)
    val topicsArg = args(1)
    val brokers = args(2)

    val inputSchema = loadSchema(schemaFile)
    logger.warn("Schema =" + inputSchema.prettyJson)

    // Set properties used to configure the producer
    val properties: Properties = new Properties()
    properties.setProperty("bootstrap.servers", brokers)
    // Set how to serialize key/value pairs
    properties.setProperty("key.serializer", "org.apache.kafka.common.serialization.LongSerializer")
    properties.setProperty("value.serializer", "org.apache.kafka.common.serialization.StringSerializer")

    val topics = topicsArg.split(",")
    logger.warn("topics="+topics.mkString(","))
    val producer: KafkaProducer[Long, String] = new KafkaProducer[Long, String](properties)

    while (true) {

      logger.warn("Produce Data # from thread #" + Thread.currentThread().getId)
      for (i <- 0.until(10)) {
        val time: Long = System.currentTimeMillis()

        val msg = DataGenerator.getRandomJson(inputSchema)

        if(i==1) {
          logger.warn("msg="+msg)
        }

        try {
          topics.foreach(topic=> {
            val record: ProducerRecord[Long, String] = new ProducerRecord[Long, String](topic, time, msg) //topic1
            producer.send(record)
          })
        }
        catch {
          case e: Exception => logger.warn(s"send failed: ${e.getMessage}")
        }
      }

      try {
        Thread.sleep(1000)
      }
      catch {
        case e: InterruptedException => Thread.currentThread().interrupt()
      }
    }
    logger.warn("Finished sending messages from thread #" + Thread.currentThread().getId + "!")
    try Thread.sleep(1000)
    catch {
      case ex: InterruptedException => Thread.currentThread().interrupt()
    }
  }


  def loadSchema(file:String)={
    val filePath = KeyVaultClient.resolveSecretIfAny(file)
    val schemaJsonString = HadoopClient.readHdfsFile(filePath).mkString("")
    DataType.fromJson(schemaJsonString)
  }
}