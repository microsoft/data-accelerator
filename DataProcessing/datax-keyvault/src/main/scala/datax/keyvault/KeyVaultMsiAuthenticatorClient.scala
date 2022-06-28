// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.keyvault

import com.azure.core.credential.{AccessToken, TokenCredential, TokenRequestContext}
import com.azure.security.keyvault.secrets.{SecretClient, SecretClientBuilder}
import datax.authentication.ManagedIdentity.getAccessToken

import java.time.OffsetDateTime
import reactor.core.publisher.Mono

class SecretClientTokenCredential extends TokenCredential {

  def getToken(request:TokenRequestContext): Mono[AccessToken] = {
		Mono.just(new AccessToken(getAccessToken("https://vault.azure.net"), OffsetDateTime.now().plusMinutes(5)))	
  }

}

object KeyVaultMsiAuthenticatorClient {

  // Get keyVaultClient that is authenticated with MSI
  def getKeyVaultClient(keyvaultName:String): SecretClient ={

    val secretClient  = new SecretClientBuilder()
            .vaultUrl(s"https://$keyvaultName.vault.azure.net")
			.credential(new SecretClientTokenCredential())
            .buildClient()
    secretClient 
  }
}
