// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.app

import datax.config.UnifiedConfig
import datax.host.BlobBatchingHost
import datax.processor.{BatchBlobProcessor, CommonProcessorFactory}

object BatchApp {
  def main(inputArguments: Array[String]): Unit = {
    startWithProcessor(inputArguments, config => CommonProcessorFactory.createProcessor(config).asBatchBlobProcessor())
  }

  def startWithProcessor(inputArguments: Array[String], processor: UnifiedConfig => BatchBlobProcessor) = {
    BlobBatchingHost.runBatchApp(
      inputArguments,
      processor)
  }
}
