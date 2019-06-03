// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.app

import com.microsoft.azure.eventhubs.EventData
import datax.host.StreamingHost
import datax.input._
import datax.processor.CommonProcessorFactory

object BlobStreamingApp {
  def main(inputArguments: Array[String]): Unit = {
    new StreamingHost[EventData].runStreamingApp(
      inputArguments,
      EventHubInputSetting.asInstanceOf[InputSetting[InputConf]],
      EventHubStreamingFactory,
      config => CommonProcessorFactory.createProcessor(config).asBlobPointerProcessor())
  }
}
