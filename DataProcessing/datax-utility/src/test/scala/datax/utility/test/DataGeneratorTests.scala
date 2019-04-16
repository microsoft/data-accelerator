// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.utility.test

import java.sql.Timestamp

import com.sun.org.apache.xalan.internal.xsltc.compiler.util.IntType
import datax.input.DataGenerator
import datax.utility.ConcurrentDateFormat
import org.apache.spark.sql.types
import org.apache.spark.sql.types._
import org.json4s.DefaultFormats
import org.json4s.JsonAST.JObject
import org.json4s.jackson.JsonMethods
import org.scalatest.{FlatSpec, Matchers}

class DataGeneratorTests extends FlatSpec with Matchers {

  implicit val formats = DefaultFormats

  // Metadata for various fields
  val doubleMetadata = new MetadataBuilder()
    .putDouble(DataGenerator.SettingMinValue, 5.0)
    .putDouble(DataGenerator.SettingMaxValue, 100.0)
    .build

  val longMetadata = new MetadataBuilder()
    .putBoolean(DataGenerator.SettingUseCurrentTimeMillis,true)
    .build

  val intMetadata = new MetadataBuilder()
    .putLongArray(DataGenerator.SettingAllowedValues,Array(3,9))
    .build

  val strMetadata = new MetadataBuilder()
    .putLong(DataGenerator.SettingMaxLength,5)
    .build

  val mapMetadata = new MetadataBuilder()
    .putLong(DataGenerator.SettingMaxLength,5)
    .build

  val arrayMetadata = new MetadataBuilder()
    .putLong(DataGenerator.SettingMaxLength,5)
    .build

  // Schema used to generate random json data
  val schema = StructType(Seq(
    StructField("doubleField", DoubleType, false, doubleMetadata),
    StructField("timeField", LongType, false, longMetadata),
    StructField("intField", IntegerType, false, intMetadata),
    StructField("stringField", StringType, false, strMetadata),
    StructField("mapField", MapType(StringType,FloatType,false), false, mapMetadata),
    StructField("arrayField", ArrayType(StringType), false, arrayMetadata)
  ))

  // Tests
  "getRandomJson" should "generate json based on double metadata correctly" in {
    val json = DataGenerator.getRandomJson(schema)
    val parsed = JsonMethods.parse(json)
    (parsed \ "doubleField").extract[Double] should (be >= 5.0 and  be <100.0)
  }

  "getRandomJson" should "generate json based on long metadata correctly" in {
    val json = DataGenerator.getRandomJson(schema)
    val parsed = JsonMethods.parse(json)
    val milliSecOneMinAgo = System.currentTimeMillis() - 60*1000L
    (parsed \ "timeField").extract[Long] should (be >= milliSecOneMinAgo and be <= System.currentTimeMillis())
  }

  "getRandomJson" should "generate json based on int metadata correctly" in {
    val json = DataGenerator.getRandomJson(schema)
    val parsed = JsonMethods.parse(json)
    (parsed \ "intField").extract[Int] should (equal (3) or equal (9))
  }


  "getRandomJson" should "generate json based on string metadata correctly" in {
    val json = DataGenerator.getRandomJson(schema)
    val parsed = JsonMethods.parse(json)
    val result = (parsed \ "stringField").extract[String]
    assert(result.length <= 5)
  }

  "getRandomJson" should "generate json based on map metadata correctly" in {
    val json = DataGenerator.getRandomJson(schema)
    val parsed = JsonMethods.parse(json)
    println(parsed)
    val result = (parsed \ "mapField").extract[Map[String, Float]]
    assert(result.size <= 5)
  }

  "getRandomJson" should "generate json based on array metadata correctly" in {
    val json = DataGenerator.getRandomJson(schema)
    val parsed = JsonMethods.parse(json)
    val result = (parsed \ "arrayField").extract[Array[String]]
    assert(result.length <= 5)
  }

}
