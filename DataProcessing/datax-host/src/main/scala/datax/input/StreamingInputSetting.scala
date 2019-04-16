// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.input

import datax.config._
import org.apache.log4j.LogManager

object StreamingInputSetting {
  case class StreamingConf(checkpointDir: String,intervalInSeconds: Long)

  val NamespaceStreaming = "streaming"
  val NamespacePrefix = SettingNamespace.JobInputPrefix + NamespaceStreaming + "."

  def buildStreamingConf(dict: SettingDictionary): StreamingConf = {
    StreamingConf(
      checkpointDir = dict.getOrNull("checkpointdir"),
      intervalInSeconds = dict.getOrElse("intervalinseconds", "0").toLong
    )
  }

  def getStreamingInputConf(dict: SettingDictionary): StreamingConf = {
    buildStreamingConf(dict.getSubDictionary(NamespacePrefix))
  }
}
