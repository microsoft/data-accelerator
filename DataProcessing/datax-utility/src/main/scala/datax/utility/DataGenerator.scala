// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.input

import java.util.concurrent.ThreadLocalRandom

import org.apache.spark.sql.types._
import org.json4s
import org.json4s.DefaultFormats
import org.json4s.JsonAST._
import org.json4s.jackson.JsonMethods.{compact, render}

import scala.util.Random

// Utility object to generate random JSON based on input schema.
object DataGenerator {

  val SettingMinValue = "minValue"
  val SettingMaxValue = "maxValue"
  val SettingMaxLength = "maxLength"
  val SettingAllowedValues = "allowedValues"
  val SettingUseCurrentTimeMillis = "useCurrentTimeMillis"
  val defaultMaxLength = 10

  def  getRandomJson(dataType: DataType): String = {
    val data  = getRandomJsonInternal(dataType, Metadata.empty)
    compact(render(data))
  }

  private def getRandomJsonInternal(dataType: DataType, metadata: Metadata): JValue = {
    implicit val formats = DefaultFormats

    val rand = ThreadLocalRandom.current()
    dataType match {
      case t: StringType =>
        val maxLength = getMaxLength(metadata)
        var result = Random.alphanumeric.take(maxLength).mkString
        if (metadata.contains(SettingAllowedValues)) {
          val vals = metadata.getStringArray(SettingAllowedValues)
          val randomIndex = rand.nextInt(vals.length)
          result = vals(randomIndex)
        }
        JString(result)
      case t: DoubleType =>
        var result = rand.nextDouble()
        if (metadata != null) {

          // The settings are evaluated in a specific order below. For e.g. is both allowedValues and Min/Max value is set, allowedValues is taken.
          // This order is maintained for all numeric types.
          if (metadata.contains(SettingAllowedValues)) {
            val vals = metadata.getDoubleArray(SettingAllowedValues)
            val randomIndex = rand.nextInt(vals.length)
            result = vals(randomIndex)
          }
          else if (metadata.contains(SettingMinValue) || metadata.contains(SettingMaxValue)) {
            val minVal = getDoubleValue(SettingMinValue, metadata)
            val maxVal = getDoubleValue(SettingMaxValue, metadata)

            result = maxVal match {
              case None => minVal match {
                case None => result
                case Some(value) => rand.nextDouble(minVal.getOrElse(Double.MinValue), maxVal.getOrElse(Double.MaxValue))
              }
              case Some(value) => rand.nextDouble(minVal.getOrElse(Double.MinValue), maxVal.getOrElse(Double.MaxValue))
            }
          }
        }
        JDouble(result)
      case t: IntegerType =>
        var result = rand.nextInt()
        if (metadata != null) {
          if(metadata.contains(SettingAllowedValues)) {
            val vals = metadata.getLongArray(SettingAllowedValues)
            val randomIndex = rand.nextInt(vals.length)
            result = vals(randomIndex).toInt
            }
          else if(metadata.contains(SettingMinValue) || metadata.contains(SettingMaxValue)) {
            val minVal = getIntValue(SettingMinValue, metadata)
            val maxVal = getIntValue(SettingMaxValue, metadata)

            result = maxVal match {
              case None => minVal match {
                case None=> result
                case Some(value) => rand.nextInt(minVal.getOrElse(Int.MinValue), maxVal.getOrElse(Int.MaxValue))
              }
              case Some(value) => rand.nextInt(minVal.getOrElse(Int.MinValue), maxVal.getOrElse(Int.MaxValue))
            }
          }
        }
        JInt(result)
      case t: LongType =>
        var result = rand.nextLong()
        if (metadata != null) {
          // currentTimeMillis setting is evaluated first, then allowedValues and then Min/Max for longType.
          if (metadata.contains(SettingUseCurrentTimeMillis)) {
            result = System.currentTimeMillis()
          }
          else if (metadata.contains(SettingAllowedValues)) {
            val vals = metadata.getLongArray(SettingAllowedValues)
            val randomIndex = rand.nextInt(vals.length)
            result = vals(randomIndex)
          }
          else if (metadata.contains(SettingMinValue) || metadata.contains(SettingMaxValue)) {
            val minVal = getLongValue(SettingMinValue, metadata)
            val maxVal = getLongValue(SettingMaxValue, metadata)

            result = maxVal match {
              case None => minVal match {
                case None=> result
                case Some(value) => rand.nextLong(minVal.getOrElse(Long.MinValue), maxVal.getOrElse(Long.MaxValue))
              }
              case Some(value) => rand.nextLong(minVal.getOrElse(Long.MinValue), maxVal.getOrElse(Long.MaxValue))
            }
          }
        }
        JInt(result)
      case t: FloatType =>
        var result = rand.nextFloat()
        if (metadata != null) {
          if (metadata.contains(SettingAllowedValues)) {
            val vals = metadata.getDoubleArray(SettingAllowedValues)
            val randomIndex = rand.nextInt(vals.length)
            result = vals(randomIndex).toFloat
          }
          else if (metadata.contains(SettingMinValue) || metadata.contains(SettingMaxValue)) {
            val minVal = getFloatValue(SettingMinValue, metadata)
            val maxVal = getFloatValue(SettingMaxValue, metadata)

            result = maxVal match {
              case None => minVal match {
                case None => result
                case Some(value) => rand.nextDouble(minVal.getOrElse(Float.MinValue).toDouble, maxVal.getOrElse(Float.MaxValue).toDouble).toFloat
              }
              case Some(value) => rand.nextDouble(minVal.getOrElse(Float.MinValue).toDouble, maxVal.getOrElse(Float.MaxValue).toDouble).toFloat
            }
          }
        }
        JDouble(result)
      case t: BooleanType =>
        JBool(rand.nextBoolean())
      case t: ArrayType =>
        val maxLength = getMaxLength(metadata)
        val size = rand.nextInt(maxLength)
        JArray(
          (0 to size).map(_ => getRandomJsonInternal(t.elementType, Metadata.empty)).toList
        )
      case t: MapType =>
        val maxLength = getMaxLength(metadata)
        val size = rand.nextInt(maxLength)
          JObject(
            (0 to size).map(_ => compact(render(getRandomJsonInternal(t.keyType, Metadata.empty))) -> getRandomJsonInternal(t.valueType, Metadata.empty)).toList
          )
      case t: StructType =>
        JObject(
          t.fields.flatMap {
            f =>
              // Generate null 10% of the time
              if (f.nullable && rand.nextInt(10)>=9)
                None
              else
                Some(JField(f.name, getRandomJsonInternal(f.dataType, f.metadata)))
          }.toList
        )
    }
  }


  private def getMaxLength(metadata: Metadata): Int = {
    var maxLength = defaultMaxLength
    if (metadata != null && metadata.contains(SettingMaxLength)) {
        maxLength = metadata.getLong(SettingMaxLength).toInt
      }
    maxLength
  }

  private def getIntValue(key: String, metadata: Metadata): Option[Int] = {
    var result: Option[Int] = None
    if (metadata != null && metadata.contains(key)) {
        result = Some(metadata.getLong(key).toInt)
    }
    result
  }

  private def getLongValue(key: String, metadata: Metadata): Option[Long] = {
    var result: Option[Long] = None
    if (metadata != null && metadata.contains(key)) {
        result = Some(metadata.getLong(key))
    }
    result
  }

  private def getDoubleValue(key: String, metadata: Metadata): Option[Double] = {
    var result: Option[Double] = None
    if (metadata != null && metadata.contains(key)) {
        result = Some(metadata.getDouble(key))
    }
    result
  }

  private def getFloatValue(key: String, metadata: Metadata): Option[Float] = {
    var result: Option[Float] = None
    if (metadata != null && metadata.contains(key)) {
        result = Some(metadata.getDouble(key).toFloat)
      }
    result
  }
}
