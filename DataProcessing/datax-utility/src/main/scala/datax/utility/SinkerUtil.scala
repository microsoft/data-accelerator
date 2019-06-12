// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.utility

import java.sql.Timestamp

import datax.constants.MetricName
import org.apache.commons.codec.digest.DigestUtils
import org.apache.log4j.LogManager
import org.apache.spark.sql.Row

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

  def outputGenerator(writer: (Seq[String], String)=>Int, sinkerName: String) = {
    (flagColumnIndex: Int) => {
      if(flagColumnIndex<0)
        (rowInfo: Row, rows: Seq[Row], partitionTime: Timestamp, partitionId: Int, loggerSuffix: String) =>
          outputAllEvents(rows,writer, sinkerName, loggerSuffix)
      else
        (rowInfo: Row, rows: Seq[Row], partitionTime: Timestamp, partitionId: Int, loggerSuffix: String) =>
          SinkerUtil.outputFilteredEvents(rows, flagColumnIndex, writer, sinkerName, loggerSuffix)
    }
  }
}
