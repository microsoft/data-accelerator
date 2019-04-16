// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.host

import datax.telemetry.AppInsightLogger
import org.apache.log4j.{Level, LogManager}
import org.apache.spark.SparkConf
import org.apache.spark.sql.SparkSession

object SparkSessionSingleton {
  def getLogger = LogManager.getLogger(this.getClass)

  @transient private var instance: SparkSession = _

  def getInstance(sparkConf: SparkConf): SparkSession = {

    if (instance == null) {
      instance = SparkSession
        .builder
        .config(sparkConf)
        .enableHiveSupport()
        .getOrCreate()
    }

    instance
  }

}
