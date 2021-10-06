// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.keyvault

import com.azure.core.credential.{AccessToken,TokenCredential,TokenRequestContext}
import com.azure.security.keyvault.secrets.{SecretClient,SecretClientBuilder}
import java.time.OffsetDateTime
import org.json4s.DefaultFormats
import org.json4s.JsonAST.JNothing
import org.json4s.jackson.JsonMethods.parse
import reactor.core.publisher.Mono

class SecretClientTokenCredential extends TokenCredential {

  // This is the local endpoint on which the HDInsight Cluster nodes will listen to for MSI access requests
  private def localMsiEndpoint="http://localhost:40381/managed/identity/oauth2/token"

  def getToken(request:TokenRequestContext): Mono[AccessToken] = {
		Mono.just(new AccessToken(getAccessToken("https://vault.azure.net"), OffsetDateTime.now().plusMinutes(5)))	
  }
  
  // Get the access token for the passed in MSI resource by calling into the local endpoint
  private def getAccessToken(resourceId:String):String ={

    implicit val formats: DefaultFormats.type = DefaultFormats

    val endpointId = s"$localMsiEndpoint?resource=$resourceId&api-version=2018-11-01"
    val token = HttpGetter.httpGet(endpointId, Option(Map("Metadata"->"true")))
    val tokenJson = parse(token)

    var tokenstr =""

    // Retrieve the access_token value from the token object json
    if (tokenJson \ "access_token" != JNothing) {
      tokenstr = (tokenJson \ "access_token").extract[String].trim
    }

    tokenstr
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
