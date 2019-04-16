// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.sink

import datax.client.cosmosdb.CosmosDBConf
import datax.config.SettingDictionary
import datax.securedsetting.KeyVaultClient

object CosmosDBOutputSetting {
  val Namespace = "cosmosdb"
  val SettingConnectionString = "connectionstring"
  val SettingDatabase = "database"
  val SettingCollection = "collection"

  def buildCosmosDBOutputConf(dict: SettingDictionary, name: String): CosmosDBConf = {
    KeyVaultClient.resolveSecretIfAny(dict.get(SettingConnectionString)) match {
      case Some(connectionString) =>
        CosmosDBConf(
          connectionString = connectionString,
          name = name,
          database = dict.getOrNull(SettingDatabase),
          collection = dict.getOrNull(SettingCollection)
        )
      case None => null
    }
  }
}
