// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.utility

import java.sql.Timestamp
import java.text.SimpleDateFormat

object DateTimeUtil {
  def getCurrentTime(): Timestamp = {
    new Timestamp(new java.util.Date().getTime)
  }

  val simpleDateTimeFormat = new SimpleDateFormat("yyyyMMdd-HHmmss")
  def formatSimple(t: Timestamp) = {
    simpleDateTimeFormat.format(t)
  }
}
