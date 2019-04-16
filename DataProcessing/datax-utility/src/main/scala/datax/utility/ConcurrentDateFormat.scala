// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.utility

import java.sql.Timestamp
import java.text.{DateFormat, SimpleDateFormat}
import java.util.Date

import org.apache.log4j.LogManager

object ConcurrentDateFormat{
  val formatStrings = Array(
    "yyyy-MM-dd'T'HH:mm:ss'Z'",
    "MM/dd/yyyy HH:mm:ss"
  )
  private val df = new ThreadLocal[Array[DateFormat]](){
    override def get(): Array[DateFormat] = super.get()

    override def initialValue(): Array[DateFormat] = formatStrings.map(new SimpleDateFormat(_))

    override def remove(): Unit = super.remove()

    override def set(value: Array[DateFormat]): Unit = super.set(value)

  }

  def tryParseStringToDate(format: DateFormat, str: String): Date = {
    try{
      format.parse(str)
    }
    catch{
      case _: Exception => null
    }
  }

  def stringToTimestamp(s: String): Timestamp = {
    if(s==null || s.isEmpty)
      null
    else{
      try{
        Timestamp.valueOf(s)
      }
      catch {
        case _: IllegalArgumentException =>
          df.get().iterator.map(f=>tryParseStringToDate(f, s)).collectFirst{
            case d: Date if d!=null => Timestamp.from(d.toInstant)
          } match {
            case Some(value) => value
            case None =>
              val logger = LogManager.getLogger("stringToTimestamp")
              logger.error(s"Failed to parse timestamp from string '$s' with data format [${formatStrings.mkString(",")}]")
              null
          }
        case e: Exception =>
          val logger = LogManager.getLogger("stringToTimestamp")
          logger.error(s"Failed to parse timestamp from string '$s'", e)
          null
      }
    }
  }
}
