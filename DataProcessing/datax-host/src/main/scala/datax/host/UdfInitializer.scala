// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.host

import datax.config.SettingDictionary
import org.apache.spark.sql.SparkSession

object UdfInitializer {
  def initialize(spark: SparkSession, dict: SettingDictionary) = {
    // Register UDF functions
    spark.udf.register("filterNull", filterNull _)
  }

  def filterNull(elems: Seq[Map[String, String]]) : Seq[Map[String, String]] = {
    elems.filter(_!=null)
  }
}
