// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.constants


object JobArgument {
  def ConfNamePrefix = s"${NamePrefix.Value}_".toUpperCase
  def ConfName_AppConf = s"${ConfNamePrefix}APPCONF"
  def ConfName_AppName = s"${ConfNamePrefix}APPNAME"
  def ConfName_LogLevel = s"${ConfNamePrefix}LOGLEVEL"
  def ConfName_DriverLogLevel = s"${ConfNamePrefix}DRIVERLOGLEVEL"
  def ConfName_CheckpointEnabled = s"${ConfNamePrefix}CHECKPOINTENABLED"
  def ConfName_AppInsightKeyRef = s"${ConfNamePrefix}APPINSIGHTKEYREF"
  def ConfName_BlobWriterTimeout: String = s"${ConfNamePrefix}BlobWriterTimeout"
  def ConfName_DefaultVaultName: String = s"${ConfNamePrefix}DEFAULTVAULTNAME"
  def ConfName_DefaultStorageAccount: String = s"${ConfNamePrefix}DEFAULTSTORAGEACCOUNT"
  def ConfName_DefaultContainer: String = s"${ConfNamePrefix}DEFAULTCONTAINER"
  def ConfName_AzureStorageJarPath: String = s"${ConfNamePrefix}AZURESTORAGEJARPATH"
}
