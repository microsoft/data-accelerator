// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.app

import datax.host.BlobBatchingHost
import datax.processor.CommonProcessorFactory

object BatchDynamicApp {
  def main(inputArguments: Array[String]): Unit = {
    BlobBatchingHost.runBatchDynamicApp(
      inputArguments,
      config => CommonProcessorFactory.createProcessor(config).asBatchBlobProcessor())
  }
}

