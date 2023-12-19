// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.input

import datax.config.{SettingDictionary, SettingNamespace}
import datax.input.StreamingInputSetting.StreamingConf
import org.apache.log4j.LogManager

object BlobPointerInputSetting {
  case class InputSource(target: String, catalogPrefix:Option[String])
  case class BlobPointerInputConf(sources: Map[String, InputSource],
                       eventhub: InputEventHubConf,
                       streaming: StreamingConf,
                       sourceIdRegex: String,
                       eventNamePath: String,
                       blobPathRegex: String,
                       fileTimeRegex: String,
                       fileTimeFormat: String)

  def NamespacePrefix = SettingNamespace.JobInputPrefix
  val SettingSourceIdRegex = "sourceidregex"
  val SettingEventNamePath = "eventnamepath"
  val SettingBlobPathRegex = "blobpathregex"
  val SettingFileTimeRegex = "filetimeregex"
  val SettingFileTimeFormat = "filetimeformat"

  val NamespaceSource = "source"
  val SettingInputSourceTarget = "target"
  val SettingInputSourceCatalogPrefix = "catalogprefix"

  private def buildInputSource(dict: SettingDictionary, name: String): InputSource = {
    InputSource(
      target = dict.getOrNull(SettingInputSourceTarget),
      catalogPrefix = dict.get(SettingInputSourceCatalogPrefix)
    )
  }

  def getInputConfig(dict: SettingDictionary): BlobPointerInputConf = {
    val logger = LogManager.getLogger(this.getClass)

    var sources: Map[String, InputSource] = null
    var eventhub: InputEventHubConf = null
    var streaming: StreamingConf = null
    var sourceIdRegex: String = null
    var eventNamePath: String = null
    var blobPathRegex: String = null
    var fileTimeRegex: String = null
    var fileTimeFormat: String = null

    dict.groupBySubNamespace(NamespacePrefix)
        .foreach{case (g, v) => {
          g match {
            case NamespaceSource => sources = v.buildConfigMap(buildInputSource)
            case EventHubInputSetting.NamespaceEventHub => eventhub = EventHubInputSetting.buildInputEventHubConf(v)
            case StreamingInputSetting.NamespaceStreaming => streaming = StreamingInputSetting.buildStreamingConf(v)
            case SettingSourceIdRegex => sourceIdRegex = v.getDefault().orNull
            case SettingEventNamePath  => eventNamePath = v.getDefault().orNull
            case SettingBlobPathRegex  => blobPathRegex = v.getDefault().orNull
            case SettingFileTimeRegex  => fileTimeRegex = v.getDefault().orNull
            case SettingFileTimeFormat => fileTimeFormat = v.getDefault().orNull
            case "blobschemafile" =>
            case groupName:String =>
              logger.warn(s"Unsupported setting group '$groupName' under namespace '$NamespacePrefix': \n ${v.getDictMap().keys.mkString("\n")}")
          }
        }}

    BlobPointerInputConf(
      sources = sources,
      eventhub = eventhub,
      streaming = streaming,
      sourceIdRegex = sourceIdRegex,
      eventNamePath = eventNamePath,
      blobPathRegex = blobPathRegex,
      fileTimeRegex = fileTimeRegex,
      fileTimeFormat = fileTimeFormat
    )
  }
}
