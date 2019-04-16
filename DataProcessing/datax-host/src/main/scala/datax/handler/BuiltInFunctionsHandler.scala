// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.handler

import datax.config.SettingDictionary
import datax.utility.{AzureFunctionCaller, ConcurrentDateFormat}
import org.apache.log4j.LogManager
import org.apache.spark.sql.SparkSession

object BuiltInFunctionsHandler {
  val logger = LogManager.getLogger(this.getClass)

  def initialize(spark: SparkSession, dict: SettingDictionary) = {
    spark.udf.register("stringToTimestamp", ConcurrentDateFormat.stringToTimestamp _)
  }
}
