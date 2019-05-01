package datax.processor

import java.sql.Timestamp
import org.apache.spark.rdd.RDD

import scala.concurrent.duration.Duration

trait StreamingProcessor[T] {
  val process: (RDD[T], Timestamp, Duration) => Map[String, Double]
}
