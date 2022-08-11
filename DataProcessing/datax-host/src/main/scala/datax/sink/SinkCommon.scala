package datax.sink

import datax.config.SparkEnvVariables
import datax.constants.MetricName
import datax.utility.DataMerger
import datax.utility.SinkerUtil.{outputAllEvents, outputFilteredEvents}
import org.apache.log4j.LogManager
import org.apache.spark.TaskContext
import org.apache.spark.sql.{DataFrame, Row}
import datax.telemetry._
import java.sql.Timestamp

object SinkCommon {
  def outputGenerator(writer: (Seq[String], String)=>Int, sinkerName: String) = {
    (flagColumnIndex: Int) => {
      if (flagColumnIndex < 0) {
        (data: DataFrame, time: Timestamp, batchTime: Timestamp, loggerSuffix: String) => {
          sinkJson(data, time, batchTime, (rowInfo: Row, rows: Seq[Row], partitionTime: Timestamp, batchTime: Timestamp, partitionId: Int, loggerSuffix: String) =>
            outputAllEvents(rows, writer, sinkerName, loggerSuffix))
        }
      }
      else {
        (data: DataFrame, time: Timestamp, batchTime: Timestamp, loggerSuffix: String) => {
          sinkJson(data, time, batchTime, (rowInfo: Row, rows: Seq[Row], partitionTime: Timestamp, batchTime: Timestamp, partitionId: Int, loggerSuffix: String) =>
            outputFilteredEvents(rows, flagColumnIndex, writer, sinkerName, loggerSuffix))
        }
      }
    }
  }

  // Convert dataframe to sequence of rows and sink using the passed in sinker delegate. The rows contain data as json column
  def sinkJson(df:DataFrame, partitionTime: Timestamp, batchTime: Timestamp, jsonSinkDelegate:JsonSinkDelegate ): Map[String, Int] = {

    df
      .rdd
      .instrumentedMapPartitions(it => {
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
          jsonSinkDelegate(rowInfo, dataAll, partitionTime, batchTime, partitionId, loggerSuffix) ++ inputMetric
        }
        else
          inputMetric
        ).iterator
      })
      .reduce(DataMerger.mergeMapOfCounts)
  }
}
