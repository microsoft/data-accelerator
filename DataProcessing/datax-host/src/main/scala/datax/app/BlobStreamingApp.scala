// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.app

import datax.host.StreamingHost
import datax.processor.CommonProcessorFactory

object BlobStreamingApp {
  def main(inputArguments: Array[String]): Unit = {
    StreamingHost.runStreamingApp(
      inputArguments,
      config => CommonProcessorFactory.createProcessor(config).asBlobPointerProcessor())
  }
}
