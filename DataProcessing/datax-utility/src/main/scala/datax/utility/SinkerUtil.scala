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
      (data: DataFrame, time: Timestamp, loggerSuffix: String) =>
        outputAllEvents(data, writer, sinkerName, loggerSuffix)
    }
  }

  def outputGenerator(writer: (Seq[String], String)=>Int, sinkerName: String) = {
    (flagColumnIndex: Int) => {
      if (flagColumnIndex < 0) {
        (data: DataFrame, time: Timestamp, loggerSuffix: String) => {
          sinkJson(data, time, (rowInfo: Row, rows: Seq[Row], partitionTime: Timestamp, partitionId: Int, loggerSuffix: String) =>
            outputAllEvents(rows, writer, sinkerName, loggerSuffix))
        }
      }
      else {
        (data: DataFrame, time: Timestamp, loggerSuffix: String) => {
          sinkJson(data, time, (rowInfo: Row, rows: Seq[Row], partitionTime: Timestamp, partitionId: Int, loggerSuffix: String) =>
            outputFilteredEvents(rows, flagColumnIndex, writer, sinkerName, loggerSuffix))
        }
      }
    }
  }

  // Convert dataframe to sequence of rows and sink using the passed in sinker delegate. The rows contain data as json column
  def sinkJson(df:DataFrame, partitionTime: Timestamp, jsonSinkDelegate:JsonSinkDelegate ): Map[String, Int] = {

    df
      .rdd
      .mapPartitions(it => {
        val partitionId = TaskContext.getPartitionId()
        val loggerSuffix = SparkEnvVariables.getLoggerSuffix()
        val logger = LogManager.getLogger(s"EventsSinker${loggerSuffix}")

        val t1 = System.nanoTime()
        var timeLast = t1
        var timeNow: Long = 0
        logger.info(s"$timeNow:Partition started")

        val dataAll = it.toArray
        val count = dataAll.length
        timeNow = System.nanoTime()
        logger.info(s"$timeNow:Collected $count events, spent time=${(timeNow - timeLast) / 1E9} seconds")
        timeLast = timeNow

        val inputMetric = Map(s"${MetricName.MetricSinkPrefix}InputEvents" -> count)
        Seq(if (count > 0) {
          val rowInfo = dataAll(0).getAs[Row](0)
          jsonSinkDelegate(rowInfo, dataAll, partitionTime, partitionId, loggerSuffix) ++ inputMetric
        }
        else
          inputMetric
        ).iterator
      })
      .reduce(DataMerger.mergeMapOfCounts)
  }
}