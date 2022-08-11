// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.utility

import java.sql.Timestamp

import datax.config.SparkEnvVariables
import datax.constants.MetricName
import datax.sink.JsonSinkDelegate
import org.apache.commons.codec.digest.DigestUtils
import org.apache.log4j.LogManager
import org.apache.spark.TaskContext
import org.apache.spark.sql.{DataFrame, Row}

object SinkerUtil {

  def boolToOnOff(on: Boolean) = {
    if(on) "ON" else "OFF"
  }

  def hashName(s: String) = {
    DigestUtils.sha256Hex(s).substring(0, 10)
  }

  def outputFilteredEvents(rows: Seq[Row], filterColumnIndex: Int, writer: (Seq[String], String)=>Int, sinkerName: String, loggerSuffix: String) = {
    val loggerPrefix = s"Sinker-$sinkerName-F$filterColumnIndex"
    val logger = LogManager.getLogger(loggerPrefix+loggerSuffix)

    val timeStart = System.nanoTime()
    val dataToSend = rows.filter(r=> !r.isNullAt(filterColumnIndex) && r.getBoolean(filterColumnIndex)).map(_.getString(1))

    val resultCount = writer(dataToSend, loggerSuffix)
    val timeNow = System.nanoTime()
    logger.info(s"$timeNow:written filtered count= $resultCount, spent time=${(timeNow - timeStart) / 1E9} seconds")
    Map(s"${MetricName.MetricSinkPrefix}${sinkerName}_Filtered" -> resultCount)
  }

  def outputAllEvents(rows: Seq[Row], writer: (Seq[String], String)=>Int, sinkerName: String, loggerSuffix: String) = {
    val loggerPrefix = s"Sinker-$sinkerName-All"
    val logger = LogManager.getLogger(loggerPrefix+loggerSuffix)

    val timeStart = System.nanoTime()
    val dataToSend = rows.map(_.getString(1))

    val resultCount = writer(dataToSend, loggerSuffix)
    val timeNow = System.nanoTime()
    logger.info(s"$timeNow:written count= $resultCount, spent time=${(timeNow - timeStart) / 1E9} seconds")
    Map(s"${MetricName.MetricSinkPrefix}${sinkerName}_All" -> resultCount)
  }

  private def outputAllEvents(dataToSend: DataFrame, writer: (DataFrame, String)=>Int, sinkerName: String, loggerSuffix: String) = {

    val loggerPrefix = s"Sinker-$sinkerName-All"
    val logger = LogManager.getLogger(loggerPrefix+loggerSuffix)
    val timeStart = System.nanoTime()

    val resultCount = writer(dataToSend, loggerSuffix)

    val timeNow = System.nanoTime()
    logger.info(s"$timeNow:written count= $resultCount, spent time=${(timeNow - timeStart) / 1E9} seconds")

    Map(s"${MetricName.MetricSinkPrefix}${sinkerName}_All" -> resultCount)
  }

  def outputGenerator(writer: (DataFrame, String)=>Int, sinkerName: String) = {

    () => {
      (data: DataFrame, time: Timestamp, batchTime: Timestamp, loggerSuffix: String) =>
        outputAllEvents(data, writer, sinkerName, loggerSuffix)
    }
  }


}