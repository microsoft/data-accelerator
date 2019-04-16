// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.sink

import datax.config.{SettingDictionary, SettingNamespace}
import datax.securedsetting.KeyVaultClient

object EventHubOutputSetting {
  case class EventHubOutputConf(connectionString: String,
                                filter: String,
                                appendProperties: Map[String, String],
                                compressionType: String,
                                format: String)

  val Namespace = "eventhub"
  val SettingConnectionString = "connectionstring"
  val SettingFilter = "filter"
  val SettingCompressionType = "compressiontype"
  val SettingFormat = "format"
  val SettingAppendProperty = "appendproperty"
  val AppendPropertyPrefix =  SettingAppendProperty + SettingNamespace.Seperator

  val FormatValueJson = "json"
  val FormatValueDefault = FormatValueJson
  val CompressionValueNone = "none"
  val CompressionValueGZip = "gzip"
  val CompressionValueDefault = CompressionValueGZip


  def buildEventHubOutputConf(dict: SettingDictionary, name: String) = {
    KeyVaultClient.resolveSecretIfAny(dict.get(SettingConnectionString)) match {
      case Some(connectionString) =>
        val properties = dict.getSubDictionary(AppendPropertyPrefix).getDictMap()
        EventHubOutputConf(
          connectionString = connectionString,
          appendProperties = properties,
          filter = dict.getOrNull(SettingFilter),
          compressionType = dict.get(SettingCompressionType).getOrElse(CompressionValueDefault),
          format = dict.get(SettingFormat).getOrElse(FormatValueDefault)
        )
      case None => null
    }
  }
}
