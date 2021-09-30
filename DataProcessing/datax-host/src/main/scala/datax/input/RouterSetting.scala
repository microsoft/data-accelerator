// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.input

import datax.config.{SettingDictionary, SettingNamespace}
import datax.securedsetting.KeyVaultClient


object RouterSetting {
  val NamespacePrefix = SettingNamespace.JobPrefix+"router."
  val NamespaceFilterJobPrefix = NamespacePrefix + "job."
  val NamespaceFilterPrefix = "filter."

  case class FilterOutput(compressionType: String, eventhub: String, folder: String, format: String, outputType: String)
  case class SPFilter(sourceIdRegex: String, mappingOperations: Map[String, String], filterCondition: String, filterType: String, jobName:String, coalescingRatio: Double, numPartitions: String, output: FilterOutput)


  def buildFilterOutput(dict: SettingDictionary) = {
    FilterOutput(
      compressionType = dict.getOrNull("compressiontype"),
      eventhub = KeyVaultClient.resolveSecretIfAny(dict.getOrNull("eventhub")),
      folder = dict.getOrNull("folder"),
      format = dict.getOrNull("format"),
      outputType = dict.getOrNull("outputType")
    )
  }

  def buildMappingOperations(s: Option[String]) = {
    s match {
      case Some(str) => str.split(";").map(p=>{
        val parts = p.split("=", 2)
        if(parts.length==2)
          parts(0).trim()->parts(1).trim()
        else
          parts(0).trim()->null
      }).toMap

      case None => null
    }
  }

  def buildFilterJob(dict: SettingDictionary, name: String) = {
    SPFilter(
      sourceIdRegex = dict.getOrNull(s"${NamespaceFilterPrefix}sourceidregex"),
      mappingOperations = buildMappingOperations(dict.get(s"${NamespaceFilterPrefix}mappingoperations")),
      filterCondition = dict.getOrNull(s"${NamespaceFilterPrefix}filterCondition"),
      filterType = dict.getOrNull(s"${NamespaceFilterPrefix}filterType"),
      jobName = name,
      coalescingRatio = dict.getDouble(s"${NamespaceFilterPrefix}coalescingRatio"),
      numPartitions = dict.getOrNull(s"${NamespaceFilterPrefix}numPartitions"),
      output = buildFilterOutput(dict.getSubDictionary(s"${NamespaceFilterPrefix}output."))
    )
  }

  def getFiltersConfig(dict: SettingDictionary) = {
    dict.buildConfigIterable(buildFilterJob, NamespaceFilterJobPrefix).toSeq
  }
}
