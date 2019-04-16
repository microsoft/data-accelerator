// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.test.classloader

import java.sql.Timestamp

import datax.classloader.ClassLoaderHost
import org.apache.spark.sql.types._
import org.scalatest.{FlatSpec, Matchers}

case class A(timestamp: Timestamp, reading: Double, category: String)

class ClassLoaderTests extends FlatSpec with Matchers{
  "infer case class to struct type" should "works" in {

    ClassLoaderHost.javaTypeToDataType(classOf[A]) shouldBe StructType(Array(
      StructField("timestamp", TimestampType, true),
      StructField("reading", DoubleType, false),
      StructField("category", StringType, true)
    ))
  }
}
