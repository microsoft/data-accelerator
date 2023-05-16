// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.sink

import datax.config.{SettingDictionary, SettingNamespace}

object BlobOutputSetting {
  case class BlobGroupOutputConf(folder: String)
  case class BlobOutputConf(groupEvaluation: Option[String], groups: Map[String, BlobGroupOutputConf], compressionType: Option[String], format: Option[String], estimatedOutputBlobSizeInBytes: Option[Long], outputBlobCount: Option[Int])

  val Namespace = "blob"
  val SettingGroupEvaluation = "groupevaluation"
  val SettingCompressionType = "compressiontype"
  val SettingFormat = "format"
  val SettingGroup = "group"
  val BlobGroupPrefix = SettingGroup + SettingNamespace.Seperator
  val SettingEstimatedOutputBlobSizeInBytes = "estimatedoutputblobsizeinbytes"
  val SettingOutputBlobCount = "outputblobcount"

  val SettingGroupOutputFolder = "folder"

  private def buildBlobGroupOutputConf(dict: SettingDictionary, name: String): BlobGroupOutputConf = {
    dict.get(SettingGroupOutputFolder).map(BlobGroupOutputConf(_)).orNull
  }

  def buildBlobOutputConf(dict: SettingDictionary, name: String): BlobOutputConf = {
    val groups = dict.buildConfigMap(buildBlobGroupOutputConf, BlobGroupPrefix)

    if(groups.size>0)
      BlobOutputConf(
        groupEvaluation = dict.get(SettingGroupEvaluation),
        groups = groups,
        compressionType = dict.get(SettingCompressionType),
        format = dict.get(SettingFormat),
        estimatedOutputBlobSizeInBytes = dict.getLongOption(SettingEstimatedOutputBlobSizeInBytes),
        outputBlobCount = dict.getIntOption(SettingOutputBlobCount)
      )
    else
      null
  }

  def getDefaultBlobOutputConf(dict: SettingDictionary): BlobOutputConf = {
    val prefix = SettingNamespace.JobOutputDefaultPreifx + Namespace + SettingNamespace.Seperator
    BlobOutputSetting.buildBlobOutputConf(dict.getSubDictionary(prefix), SettingNamespace.JobOutputDefaultPreifx + Namespace)
  }
}
