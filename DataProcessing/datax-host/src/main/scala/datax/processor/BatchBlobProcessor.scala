package datax.processor

import java.sql.Timestamp
import datax.utility.DateTimeUtil
import org.apache.spark.rdd.RDD

import scala.concurrent.duration.Duration

class BatchBlobProcessor(processBatchBlobPaths: (RDD[String], Timestamp, Duration, Timestamp, String) => Map[String, Double]) {

    val process = (rdd: RDD[String], batchTime: Timestamp, batchInterval: Duration) => {
      processBatchBlobPaths(rdd, batchTime, batchInterval, batchTime, "")
  }
}
