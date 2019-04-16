// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.sample.udaf

import org.apache.spark.sql.Row
import org.apache.spark.sql.expressions.{MutableAggregationBuffer, UserDefinedAggregateFunction}
import org.apache.spark.sql.types._

class UdafLastThreshold() extends UserDefinedAggregateFunction {
  def deterministic: Boolean = true

  override def inputSchema: StructType = StructType(Array(
    StructField("eventTime", TimestampType),
    StructField("thresholdType", StringType),
    StructField("val1", StringType),
    StructField("val2", StringType)
  ))

  def compare(buffer: MutableAggregationBuffer, input: Row) = {
    buffer.getTimestamp(0).compareTo(input.getTimestamp(0))<=0
  }

  def dataType: DataType = inputSchema

  val pivotsCount = this.inputSchema.fields.length - 1
  def bufferSchema = inputSchema

  def initialize(buffer: MutableAggregationBuffer) = {
    buffer(0)=null //Timestamp.from(Instant.now())
    buffer(1)=null
    buffer(2)=null
    buffer(3)=null
  }

  val inputFieldsCount = inputSchema.fields.length

  def update(buffer: MutableAggregationBuffer, input: Row) = {
    if(!input.isNullAt(0) && (buffer.isNullAt(0) || compare(buffer, input))) {
      var i = 0
      for(v <- input.toSeq){
        buffer(i)=v
        i+=1
      }
    }
  }

  def merge(buffer1: MutableAggregationBuffer, buffer2: Row) = {
    update(buffer1, buffer2)
  }

  def evaluate(buffer: Row): Any = {
    buffer
  }
}


