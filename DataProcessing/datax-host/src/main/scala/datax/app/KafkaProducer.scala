package datax.app

import org.apache.kafka.clients.producer._
import org.apache.kafka.clients.admin.AdminClient
import org.apache.kafka.clients.admin.DescribeTopicsResult
import org.apache.kafka.clients.admin.KafkaAdminClient
import org.apache.kafka.clients.CommonClientConfigs
import org.apache.kafka.clients.admin.TopicDescription
import java.util.Collection
import java.util.concurrent.ExecutionException
import java.util.Properties
import java.util.Random
import java.io.IOException
import scala.collection.JavaConversions._

object KafkaProducer {
  def main(inputArguments: Array[String]): Unit = {
    val brokers: String =
      "wn0-dxtest.yjzseuk1sqnu3bncdldk5etavb.jx.internal.cloudapp.net:9092,wn1-dxtest.yjzseuk1sqnu3bncdldk5etavb.jx.internal.cloudapp.net:9092"
    // Set properties used to configure the producer
    val properties: Properties = new Properties()
    // Set the brokers (bootstrap servers)
    properties.setProperty("bootstrap.servers", brokers)
   // properties.setProperty("acks", "all")
   // properties.setProperty("linger.ms", "1")
  //  properties.setProperty("max.block.ms", "5000")


    // Set how to serialize key/value pairs
    properties.setProperty(
      "key.serializer",
      "org.apache.kafka.common.serialization.LongSerializer")
    properties.setProperty(
      "value.serializer",
      "org.apache.kafka.common.serialization.StringSerializer")
    val producer: KafkaProducer[Long, String] =
      new KafkaProducer[Long, String](properties)
    val r: Random = new Random()
    while (true) {
      for (i <- 0.until(10)) {
        val time: Long = System.currentTimeMillis()
        println(
          "Test Data #" + i + " from thread #" + Thread.currentThread().getId)
        val msg: String =
          "{\"temperature\":%d,\"eventTime\":%d}".format(
            r.nextLong(),
            time)
        val record: ProducerRecord[Long, String] =
          new ProducerRecord[Long, String]("topic1", time, msg)
        producer.send(record)
        try Thread.sleep(1000)
        catch {
          case ex: InterruptedException => Thread.currentThread().interrupt()

        }
      }
      println(
        "Finished sending messages from thread #" + Thread
          .currentThread()
          .getId +
          "!")
      try Thread.sleep(1000)
      catch {
        case ex: InterruptedException => Thread.currentThread().interrupt()

      }
    }

  }

}
