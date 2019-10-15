// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.sink

import datax.client.sql.SqlConf
import datax.config.SettingDictionary
import datax.securedsetting.KeyVaultClient


object SqlOutputSetting {
  val Namespace = "sql"
  val SettingConnectionString = "connectionstring"
  val SettingUrl="url"
  val SettingEncrypt = "encrypt"
  val SettingTrustServerCertificate = "trustServerCertificate"
  val SettingHostNameInCertificate = "hostNameInCertificate"
  val SettingDatabaseName = "databasename"
  val SettingTable = "table"
  val SettingFilter="filter"
  val SettingWriteMode="writemode"
  val SettingUserName = "user"
  val SettingPassword = "password"
  val SettingConnectionTimeout = "connectiontimeout"
  val SettingQueryTimeout = "querytimeout"
  val SettingUseBulkCopy = "usebulkinsert"
  val SettingUseBulkCopyTablelock = "usebulkcopytablelock"
  val SettingUseBulkCopyInternalTransaction = "usebulkcopyinternaltransaction"
  val SettingUseBulkCopyTimeout = "bulkcopytimeout"
  val SettingUseBulkBatchSize = "bulkcopybatchsize"

  def buildSqlOutputConf(dict: SettingDictionary, name: String): SqlConf = {
    KeyVaultClient.resolveSecretIfAny(dict.get(SettingConnectionString)) match {
      case Some(connectionString) =>
        SqlConf(
          name = name,
          connectionString = connectionString,
          url = KeyVaultClient.resolveSecretIfAny(dict.getOrNull(SettingUrl)),
          encrypt = dict.getOrNull(SettingEncrypt),
          trustServerCertificate = dict.getOrNull(SettingTrustServerCertificate),
          hostNameInCertificate = dict.getOrNull(SettingHostNameInCertificate),
          databaseName = dict.getOrNull(SettingDatabaseName),
          table = dict.getOrNull(SettingTable),
          writeMode = dict.getOrElse(SettingWriteMode,"append"),
          userName = KeyVaultClient.resolveSecretIfAny(dict.getString(SettingUserName)),
          password = KeyVaultClient.resolveSecretIfAny(dict.getString(SettingPassword)),
          filter = dict.getOrNull(SettingFilter),
          connectionTimeout=dict.getOrElse(SettingConnectionTimeout,"30"),
          queryTimeout = dict.getOrElse(SettingQueryTimeout,"30"),
          useBulkCopy = dict.getOrElse(SettingUseBulkCopy,"false").toBoolean,
          useBulkCopyTableLock = dict.getOrElse(SettingUseBulkCopyTablelock,"false"),
          useBulkCopyInternalTransaction = dict.getOrElse(SettingUseBulkCopyInternalTransaction,"false"),
          bulkCopyTimeout = dict.getOrElse(SettingUseBulkCopyTimeout,"60"),
          bulkCopyBatchSize = dict.getOrElse(SettingUseBulkBatchSize,"2000")
        )
      case None => null
    }
  }

}
