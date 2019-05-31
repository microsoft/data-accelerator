package datax.processor

import java.sql.Timestamp
import datax.utility.DateTimeUtil
import org.apache.spark.rdd.RDD

import scala.concurrent.duration.Duration

class BlobProcessor(processBatchBlobPaths: (RDD[String], Timestamp, Duration, Timestamp, String) => Map[String, Double]) {

    val process = (rdd: RDD[String], batchTime: Timestamp, batchInterval: Duration) => {
    val currentTime = DateTimeUtil.getCurrentTime()
      processBatchBlobPaths(rdd, batchTime, batchInterval, currentTime, "")
  }
}
