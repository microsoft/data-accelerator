// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.extension

import datax.config.SettingDictionary
import org.apache.spark.sql.{DataFrame, SparkSession}

trait PreProjectionProcessor extends Serializable{
  def initialize(spark:SparkSession, dict: SettingDictionary): DataFrame=>DataFrame
}
