// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.host

import datax.service.{ConfigService, TelemetryService}
import org.apache.spark.SparkConf
import org.apache.spark.sql.SparkSession

trait AppHost {
  def getConfigService(): ConfigService
  def getTelemetryService(): TelemetryService
  def getSpark(sparkConf: SparkConf): SparkSession
}
