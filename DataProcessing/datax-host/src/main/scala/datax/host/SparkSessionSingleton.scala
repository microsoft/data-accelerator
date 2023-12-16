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
    val logger = LogManager.getLogger(s"getInstance")
    if (instance == null) {
      var builder = SparkSession
        .builder
        .config(sparkConf)
      val isLocalMode = sparkConf.getOption("spark.master").contains("local")
      if(!isLocalMode) {
        builder = builder.enableHiveSupport()
      }
      else {
        logger.warn("Spark executing in local mode")
      }
      instance = builder.getOrCreate()
    }

    instance
  }

}
