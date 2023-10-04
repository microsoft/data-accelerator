// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.app

import datax.host.{BlobBatchingHost, SparkSessionSingleton}
import datax.processor.CommonProcessorFactory
import org.apache.spark.sql.SparkSession

object BatchApp {

  /**
   * Creates a new batch app
   * @param inputArguments The command line argument for the batch app
   * @param sparkSession An optional spark session, if null, a spark session is created for the caller
   */
  def create(inputArguments: Array[String], sparkSession: SparkSession = null): Unit = {
    if(sparkSession != null) {
      // If provided, inject the session and commit it, so it will referenced later
      SparkSessionSingleton.setInstance(sparkSession)
    }
    BlobBatchingHost.runBatchApp(
      inputArguments,
      config => CommonProcessorFactory.createProcessor(config).asBatchBlobProcessor())
  }
  def main(inputArguments: Array[String]): Unit = {
    create(inputArguments)
  }
}
