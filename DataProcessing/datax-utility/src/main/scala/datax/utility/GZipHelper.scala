// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.utility

import java.io.{ByteArrayInputStream, ByteArrayOutputStream}
import java.util.zip.{GZIPInputStream, GZIPOutputStream}

import org.apache.commons.codec.binary.Base64
import org.apache.commons.io.IOUtils

object GZipHelper {

  def deflate(txt: String): String = {
      Base64.encodeBase64String(deflateToBytes(txt))
  }

  val newLineByte = "\n".getBytes()

  def deflateToBytes(lines: Seq[String]): Array[Byte] = {
    val arrOutputStream = new ByteArrayOutputStream()
    val zipOutputStream = new GZIPOutputStream(arrOutputStream)
    var firstLine = true
    lines.foreach(l=> {
      if(firstLine){
        firstLine=false
      }
      else{
        zipOutputStream.write(newLineByte)
      }

      zipOutputStream.write(l.getBytes)
    })

    zipOutputStream.close()
    arrOutputStream.toByteArray
  }

  def deflateToBytes(txt: String): Array[Byte] = {
    val arrOutputStream = new ByteArrayOutputStream()
    val zipOutputStream = new GZIPOutputStream(arrOutputStream)
    zipOutputStream.write(txt.getBytes)
    zipOutputStream.close()
    arrOutputStream.toByteArray
  }

  def inflate(deflatedTxt: String): String = {

      val bytes = Base64.decodeBase64(deflatedTxt)
      val zipInputStream = new GZIPInputStream(new ByteArrayInputStream(bytes))
      IOUtils.toString(zipInputStream)
  }

  def inflateBytes(inputBytes : Array[Byte]) : String = {
    val zipInputStream = new GZIPInputStream(new ByteArrayInputStream(inputBytes))
    IOUtils.toString(zipInputStream)
  }
}
