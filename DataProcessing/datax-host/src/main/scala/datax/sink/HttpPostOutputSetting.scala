// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.sink

import datax.config.{SettingDictionary, SettingNamespace}

object HttpPostOutputSetting {
  case class HttpPostConf(endpoint: String, filter: String, appendHeaders: Option[Map[String, String]])

  val Namespace = "httppost"
  val SettingEndpoint = "endpoint"
  val SettingFilter = "filter"
  val SettingHeader = "header"
  val AppendHeaderPrefix = SettingHeader+SettingNamespace.Seperator

  def getHttpPostConf(dict: SettingDictionary, name: String) = {
    if(dict.size>0) {
      val headers = dict.getSubDictionary(AppendHeaderPrefix).getDictMap()
      HttpPostConf(
        endpoint = dict.getOrNull(SettingEndpoint),
        filter = dict.getOrNull(SettingHeader),
        appendHeaders = Option(headers)
      )
    }
    else
      null
  }
}
