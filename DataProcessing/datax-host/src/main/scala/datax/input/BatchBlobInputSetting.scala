// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.input

import datax.config.{SettingDictionary, SettingNamespace}
import datax.securedsetting.KeyVaultClient
import org.apache.log4j.LogManager

case class InputBlobsConf(path:String,
                         startTime: String,
                         endTime: String,
                         format: String,
                         compression: String,
                         partitionIncrementInMin:Long)

object BatchBlobInputSetting {
  val NamespaceBlobsSource = "blob"
  val NamespacePrefix = SettingNamespace.JobInputPrefix + NamespaceBlobsSource + SettingNamespace.Seperator

  val SettingPath = "path"
  val SettingProcessStartTime = "processstarttime"
  val SettingProcessEndTime = "processendtime"
  val SettingPartitionIncrement = "partitionincrement"
  val SettingFormat = "format"
  val SettingCompression = "compression"

  val logger = LogManager.getLogger(this.getClass)

  private def buildInputBlobsConf(dict: SettingDictionary, name: String): InputBlobsConf = {
    logger.warn("Load Dictionary from buildInputBlobsConf as following:\n"+dict.dict.map(kv=>s"${kv._1}->${kv._2}").mkString("\n"))

    InputBlobsConf(
      path = KeyVaultClient.resolveSecretIfAny(dict.getOrNull(SettingPath)),
      startTime = dict.getOrNull(SettingProcessStartTime),
      endTime = dict.getOrNull(SettingProcessEndTime),
      format = dict.getOrNull(SettingFormat),
      compression = dict.getOrNull(SettingCompression),
      partitionIncrementInMin = dict.getLong(SettingPartitionIncrement)
    )
  }

  def getInputBlobsArrayConf(dict: SettingDictionary): Seq[InputBlobsConf] = {
    logger.warn("Blob namespace="+NamespacePrefix)
    dict.buildConfigIterable(buildInputBlobsConf, NamespacePrefix).toSeq
  }
}
