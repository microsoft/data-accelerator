// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.handler

import datax.config.{SettingDictionary, SettingNamespace}
import datax.exception.EngineException
import org.apache.log4j.LogManager
import org.apache.spark.sql.{Column, SparkSession}
import org.apache.spark.sql.functions.col

import scala.concurrent.duration.Duration

case class TimeWindowConf(
                         windows: Map[String, Duration],
                         isEnabled: Boolean,
                         timestampColumn: Column,
                         watermark: Duration,
                         maxWindow: Duration
                         )

object TimeWindowHandler {
  val NamespacePrefix=SettingNamespace.JobProcessPrefix
  val SettingWatermark = "watermark"
  val SettingTimestampColumn = "timestampcolumn"
  val SettingTimeWindow = "timewindow"
  val SettingTimeWindowDuration = "windowduration"

  val logger = LogManager.getLogger(this.getClass)


  private def buildTimeWindowConf(dict: SettingDictionary, name: String) = {
    dict.getDuration(SettingTimeWindowDuration)
  }

  private def buildTimeWindowsConf(dict: SettingDictionary, prefix: String)= {
    dict.buildConfigMap(buildTimeWindowConf, prefix)
  }

  def initialize(spark: SparkSession, dict: SettingDictionary) = {
    val windows = buildTimeWindowsConf(dict, NamespacePrefix + SettingTimeWindow + SettingNamespace.Seperator)
    val watermark = dict.get(NamespacePrefix + SettingWatermark)
    val timestampColumn = dict.get(NamespacePrefix + SettingTimestampColumn)
    val isEnabled = windows.size > 0 && watermark.isDefined && timestampColumn.isDefined

    if (isEnabled) {
      logger.warn(s"Windowing is ON, window are ${windows}, watermark is ${watermark}")
      TimeWindowConf(
        windows = windows,
        isEnabled = windows.size > 0 && watermark.isDefined && timestampColumn.isDefined,
        timestampColumn =col(timestampColumn.get),
        watermark = Duration.create(watermark.get),
        maxWindow = windows.values.max
      )
    }
    else {
      logger.warn(s"Windowing is OFF")
      TimeWindowConf(
        null,
        false,
        null,
        Duration.Zero,
        Duration.Zero
      )
    }
  }
}
