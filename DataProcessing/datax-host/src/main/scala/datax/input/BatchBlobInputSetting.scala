// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.input

import datax.config.ConfigManager.logger
import datax.config.{SettingDictionary, SettingNamespace}
import org.apache.log4j.LogManager

import scala.collection.mutable

/*
case class InputBlobsConf(pathPrefix:String,
                          pathPartitionFolderFormat: String,
                          startTime: String,
                          durationInHours: Long)

object BatchBlobInputSetting {
  val NamespaceBlobsSource = "blobs"
  val NamespacePrefix = SettingNamespace.JobInputPrefix + NamespaceBlobsSource + SettingNamespace.Seperator

  val logger = LogManager.getLogger(this.getClass)

  private def buildInputBlobsConf(dict: SettingDictionary, name: String): InputBlobsConf = {
    logger.warn("Load Dictionary from buildInputBlobsConf as following:\n"+dict.dict.map(kv=>s"${kv._1}->${kv._2}").mkString("\n"))
    logger.warn("buildInputBlobsConf name="+name);
    InputBlobsConf(
      pathPrefix = dict.getOrNull("pathprefix"),
      pathPartitionFolderFormat = dict.getOrNull("pathpartitionfolderformat"),
      startTime = dict.getOrNull("starttime"),
      durationInHours = dict.getLong("durationinhours")
    )
  }
  */

case class InputBlobsConf(path:String,
                         startTime: String)

object BatchBlobInputSetting {
  val NamespaceBlobsSource = "blobs"
  val NamespacePrefix = SettingNamespace.JobInputPrefix + NamespaceBlobsSource + SettingNamespace.Seperator

  val logger = LogManager.getLogger(this.getClass)

  private def buildInputBlobsConf(dict: SettingDictionary, name: String): InputBlobsConf = {
    logger.warn("Load Dictionary from buildInputBlobsConf as following:\n"+dict.dict.map(kv=>s"${kv._1}->${kv._2}").mkString("\n"))
    logger.warn("buildInputBlobsConf name="+name);
    InputBlobsConf(
      path = dict.getOrNull("path"),
      startTime = dict.getOrNull("starttime")
    )
  }

  def getInputBlobsArrayConf(dict: SettingDictionary): Seq[InputBlobsConf] = {
    logger.warn("Blob namespace="+NamespacePrefix)
    dict.buildConfigIterable(buildInputBlobsConf, NamespacePrefix).toSeq
  }
}
