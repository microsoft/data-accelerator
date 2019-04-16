// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.sample.udf

import org.apache.spark.sql.api.java.{UDF1 => jUDF}

class UdfHelloWorld extends jUDF[String, String]{
  override def call(p1: String): String = {
    "hello, " + p1
  }
}
