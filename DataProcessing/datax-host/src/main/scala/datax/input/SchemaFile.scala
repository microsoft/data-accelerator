// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.input

import datax.config.ConfigManager.loadConfigFile
import datax.config.{ConfigManager, SettingDictionary, SettingNamespace}
import datax.fs.HadoopClient
import datax.securedsetting.KeyVaultClient
import org.apache.log4j.LogManager
import org.apache.spark.sql.types.{DataType, StructType}

import scala.concurrent.{ExecutionContext, Future}

object SchemaFile {
  val SettingSchemaFile="blobschemafile"

 private def getInputBlobSchemaFilePath(dict: SettingDictionary) = {
    dict.getOrNull(SettingNamespace.JobInputPrefix + SettingSchemaFile)
  }

 private def loadRawBlobSchema(blobSchemaFile: String) = {
    // Schema of VS block extraction data
    val schemaJsonString = loadConfigFile(blobSchemaFile).mkString("")
    DataType.fromJson(schemaJsonString)
  }

  def loadInputSchema(dict: SettingDictionary) = {
    val file = getInputBlobSchemaFilePath(dict)
    val logger = LogManager.getLogger(this.getClass)
    logger.warn(s"Load input schema from '${file}'")
    val filePath = KeyVaultClient.resolveSecretIfAny(file)
    loadRawBlobSchema(filePath)
  }
}
