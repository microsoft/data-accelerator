package datax.telemetry

import org.apache.spark.rdd.RDD

import scala.reflect.ClassTag

class InstrumentedRDDFunctions[T](rdd: RDD[T]) extends AnyRef with Serializable {

  @transient implicit private val sc = rdd.sparkContext

  def instrumentedMap[U](f: (T) ⇒ U)(implicit ct: ClassTag[U]): RDD[U] = {
    rdd.map(x => AppInsightLogger.InstrumentedFunction((Unit) => {
      f(x)
    }, "UncaughtException"))
  }
  def instrumentedFlatMap[U](f: (T) ⇒ TraversableOnce[U])(implicit ct: ClassTag[U]): RDD[U] = {
    rdd.flatMap(x => AppInsightLogger.InstrumentedFunction((Unit) => {
      f(x)
    }, "UncaughtException"))
  }

  def instrumentedMapPartitions[U: ClassTag](f: Iterator[T] => Iterator[U], preservesPartitioning: Boolean = false): RDD[U] = {
    rdd.mapPartitions(x => AppInsightLogger.InstrumentedFunction((Unit) => {
      f(x)
    }, "UncaughtException"))
  }

}


