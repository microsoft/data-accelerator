// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.sample.dynamicudf

import java.sql.Timestamp
import java.time.Instant

import datax.config.SettingDictionary
import datax.extension.DynamicUDF
import datax.extension.DynamicUDF.{Generator1, UDF1}
import org.apache.spark.sql.SparkSession

class DynamicUdfHelloWorld extends Generator1[String, String]{
  var internalCounter = 1
  var timestamp = Timestamp.from(Instant.now)

  override def initialize(spark: SparkSession, dict: SettingDictionary): DynamicUDF.UDF1[String, String] = {
    UDF1(
      func = s=>s"Hello $s, my counter is $internalCounter, timestamp at ${timestamp}",
      onInterval = (spark, time) =>{
        timestamp = time
        internalCounter+=1
      }
    )
  }
}
