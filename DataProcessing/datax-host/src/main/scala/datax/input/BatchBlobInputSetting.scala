// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.input

import datax.config.{SettingDictionary, SettingNamespace}
import org.apache.log4j.LogManager

import scala.collection.mutable

case class InputBlobsConf(pathPrefix:String,
                          pathPartitionFolderFormat: String,
                          startTime: String,
                          durationInHours: Long)

object BatchBlobInputSetting {
  val NamespaceBlobsSource = "blobs"
  val NamespacePrefix = SettingNamespace.JobInputPrefix+NamespaceBlobsSource+"."

  private def buildInputBlobsConf(dict: SettingDictionary, name: String): InputBlobsConf = {
    InputBlobsConf(
      pathPrefix = dict.getOrNull("pathprefix"),
      pathPartitionFolderFormat = dict.getOrNull("pathpartitionfolderformat"),
      startTime = dict.getOrNull("starttime"),
      durationInHours = dict.getLong("durationinhours")
    )
  }

  def getInputBlobsArrayConf(dict: SettingDictionary): Seq[InputBlobsConf] = {
     dict.buildConfigIterable(buildInputBlobsConf, NamespacePrefix).toSeq
  }
}
