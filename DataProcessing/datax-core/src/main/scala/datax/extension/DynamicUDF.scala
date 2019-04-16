// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.extension

import java.sql.Timestamp

import datax.config.SettingDictionary
import org.apache.spark.sql.SparkSession

object DynamicUDF{
  type IntervalUpdateHandler = (SparkSession, Timestamp)=>Unit

  trait DynamicUDFTrait {
    val name: String
    val funcRef: AnyRef
    val onInterval: IntervalUpdateHandler
  }

  case class UDF0[RT] (func: ()=>RT, onInterval: IntervalUpdateHandler = null)

  case class UDF1[T1, RT] (func: T1=>RT, onInterval: IntervalUpdateHandler = null)

  case class UDF2[T1, T2, RT] (func: (T1, T2) => RT, onInterval: IntervalUpdateHandler = null)

  case class UDF3[T1, T2, T3, RT] (func: (T1, T2, T3) => RT,onInterval: IntervalUpdateHandler = null)

  case class UDF4[T1, T2, T3, T4, RT] (func: (T1, T2, T3, T4) => RT,onInterval: IntervalUpdateHandler = null)

  trait Generator0[RT] extends Serializable{
    def initialize(spark:SparkSession, dict: SettingDictionary): UDF0[RT]
  }

  trait Generator1[T1, RT] extends Serializable {
    def initialize(spark:SparkSession, dict: SettingDictionary): UDF1[T1, RT]
  }

  trait Generator2[T1, T2, RT] extends Serializable {
    def initialize(spark:SparkSession, dict: SettingDictionary): UDF2[T1, T2, RT]
  }

  trait Generator3[T1, T2, T3, RT] extends Serializable {
    def initialize(spark:SparkSession, dict: SettingDictionary): UDF3[T1, T2, T3, RT]
  }
}


