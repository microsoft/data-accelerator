package datax.processor

import datax.telemetry.AppInsightLogger

import java.sql.Timestamp
import datax.utility.DateTimeUtil
import org.apache.spark.rdd.RDD

import scala.concurrent.duration.Duration

class BatchBlobProcessor(processBatchBlobPaths: (RDD[String], Timestamp, Duration, Timestamp, String) => Map[String, Double]) {

    val process = (rdd: RDD[String], batchTime: Timestamp, batchInterval: Duration) => {
      AppInsightLogger.InstrumentedFunction((Unit) => processBatchBlobPaths(rdd, batchTime, batchInterval, batchTime, ""), "UncaughtException")
  }
}
