// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.keyvault

import com.microsoft.azure.keyvault.KeyVaultClient

// Factory that instantiates MSI authenticated KeyVaultClient/
// This assumes there is only one MSI associated with the Cluster.
object KeyVaultClientFactory {

  private var kvClient: KeyVaultClient = null

  def getKeyVaultClient(): KeyVaultClient = {

    if(kvClient == null){
      this.synchronized {
        if(kvClient == null){

          kvClient = KeyVaultMsiAuthenticatorClient.getKeyVaultClient()
        }
      }
    }
    kvClient
  }
}
