// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.constants

object NamePrefix {
  val DefaultValue =  "DataX"
  val ConfSetting = "DATAX_NAMEPREFIX"
  def Value: String = sys.env.getOrElse(ConfSetting, DefaultValue)
}
