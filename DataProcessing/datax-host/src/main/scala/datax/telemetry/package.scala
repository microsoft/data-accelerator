package datax

import org.apache.spark.rdd.RDD

import scala.language.implicitConversions

package object telemetry {
  implicit def rddToInstrumentedRDDFunctions[T](rdd: RDD[T]): InstrumentedRDDFunctions[T] = new InstrumentedRDDFunctions[T](rdd)
}
