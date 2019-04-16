// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.utility

import org.apache.log4j.{Level, Logger}
import org.apache.avro.Schema
import org.apache.avro.data.Json
import org.apache.avro.io.EncoderFactory
import org.apache.commons.io.output.ByteArrayOutputStream

object StreamingUtility {

  /** Set reasonable logging levels for streaming if the user has not configured log4j. */

  def setStreamingLogLevels() {

    val log4jInitialized = Logger.getRootLogger.getAllAppenders.hasMoreElements

    if (!log4jInitialized) {

      Logger.getRootLogger.setLevel(Level.WARN)

    }

  }

  def GetIpOctate(ip:String,index:Int):Int =
  {
    var octateNum = 0
    if(ip!=null && ip!="")
    {
      val arr = ip.split("""\.""")
      if(arr.length > index)
      {
        val octate = arr(index)
        octateNum =  octate.toInt
      }
    }
    octateNum
  }

  def getBinaryJson(json:String) : Array[Byte] = {
    val node = Schema.parseJson(json)
    val out = new ByteArrayOutputStream()
    val writer = new Json.Writer()
    val encoder = EncoderFactory.get().binaryEncoder(out, null)
    // encoder = EncoderFactory.get().validatingEncoder(Json.SCHEMA, encoder)
    writer.write(node, encoder)
    encoder.flush()
    val bytes = out.toByteArray()
    return  bytes
  }

}
