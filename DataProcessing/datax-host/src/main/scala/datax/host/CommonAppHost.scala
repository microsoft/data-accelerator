// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.host

import datax.config.{ConfigManager,  UnifiedConfig}
import datax.constants._
import datax.fs.HadoopClient
import datax.service.{ConfigService, TelemetryService}
import datax.telemetry.AppInsightLogger
import org.apache.log4j.LogManager
import org.apache.spark.SparkConf
import org.apache.spark.sql.SparkSession

object CommonAppHost extends AppHost {
  override def getConfigService(): ConfigService = ConfigManager
  override def getTelemetryService(): TelemetryService = AppInsightLogger

  def initApp(inputArguments: Array[String]): (AppHost, UnifiedConfig) = {
    val appLog = LogManager.getLogger(this.getClass)
    appLog.warn("===App log turned ON===")

    val sparkConf = ConfigManager.initSparkConf

    // Get the singleton instance of SparkSession
    val spark = SparkSessionSingleton.getInstance(sparkConf)

    val conf = ConfigManager.getConfigurationFromArguments(inputArguments)

    // Initialize FileSystemUtil
    HadoopClient.setConf(spark.sparkContext.hadoopConfiguration)

    appLog.warn(s"initializing with conf:"+ conf.toString)

    AppInsightLogger.initForApp(spark.sparkContext.appName)
    conf.getDriverLogLevel() match {
      case Some(level) => Logger.setLogLevel(level)
      case None =>
    }

    AppInsightLogger.trackEvent(DatasetName.DataStreamProjection + "/app/init")

    val unifiedConfig = ConfigManager.loadConfig(sparkConf)

    (this, unifiedConfig)
  }

  def getSpark(sparkConf: SparkConf): SparkSession = {
    SparkSessionSingleton.getInstance(sparkConf)
  }
}
