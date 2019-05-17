// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.app

import datax.host.StreamingHost
import datax.input._
import datax.processor.CommonProcessorFactory
import org.apache.kafka.clients.consumer.ConsumerRecord

// Main entry point class for processing data from Kafka input
object DirectKafkaStreamingApp {
  def main(inputArguments: Array[String]): Unit = {
    new StreamingHost[ConsumerRecord[String,String]]().runStreamingApp(
      inputArguments,
      KafkaInputSetting.asInstanceOf[InputSetting[InputConf]],
      KafkaStreamingFactory,
      config => CommonProcessorFactory.createProcessor(config).asDirectKafkaProcessor())
  }
}
