package datax.processor

import java.sql.Timestamp
import datax.utility.DateTimeUtil
import org.apache.spark.rdd.RDD

import scala.concurrent.duration.Duration

class BatchBlobProcessor(processBatchBlobPaths: (RDD[String], Timestamp, Duration, Timestamp, String, Long) => Map[String, Double]) {

    val process = (rdd: RDD[String], batchTime: Timestamp, batchInterval: Duration, inputPartitionSizeThresholdInBytes: Long) => {
      processBatchBlobPaths(rdd, batchTime, batchInterval, batchTime, "", inputPartitionSizeThresholdInBytes)
  }
}
