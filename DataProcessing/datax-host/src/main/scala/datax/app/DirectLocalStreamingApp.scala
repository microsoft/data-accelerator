// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.app

import com.microsoft.azure.eventhubs.EventData
import datax.host.StreamingHost
import datax.input._
import datax.processor.CommonProcessorFactory

object DirectLocalStreamingApp {

  def main(inputArguments: Array[String]): Unit = {
  new StreamingHost[EventData]().runStreamingApp(
    inputArguments,
    LocalInputSetting.asInstanceOf[InputSetting[InputConf]],
    LocalStreamingFactory,
    config => CommonProcessorFactory.createProcessor(config).asDirectLocalProcessor())
}
}
