// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.client.eventhub

import java.net.URI
import java.util.Collections
import java.util.concurrent.{CompletableFuture, Executors}
import java.util.function
import com.microsoft.azure.eventhubs.EventHubClient
import org.apache.spark.eventhubs.ConnectionStringBuilder
import org.apache.spark.eventhubs.utils.AadAuthenticationCallback
import com.microsoft.aad.msal4j.{IAuthenticationResult, _}
import datax.securedsetting.KeyVaultClient

object EventHubBase {
  // get AAD configs
  val tenantId = KeyVaultClient.resolveSecretIfAny(sys.env.getOrElse("AAD_TENANTID", ""))
  val clientId = KeyVaultClient.resolveSecretIfAny(sys.env.getOrElse("AAD_CLIENTID", ""))
  val clientSecret = KeyVaultClient.resolveSecretIfAny(sys.env.getOrElse("AAD_CLIENTSECRET", ""))

  val executorService = Executors.newSingleThreadScheduledExecutor()

  def buildConnectionString(namespace: String, name: String, policy: String, key: String) = {
    ConnectionStringBuilder()
      .setNamespaceName(namespace)
      .setEventHubName(name)
      .setSasKeyName(policy)
      .setSasKey(key)
      .build
  }

  def extractConnectionString(connString: String) : Map[String, String] = {
    var extracted = Map[String, String]()
    connString.split(";").foreach(s => {
      val parts = s.split("=")
      if (parts.length == 2) {
        extracted += (parts(0) -> parts(1))
      }
    })
    extracted
  }

  def buildClientFromConnString(extracted: Map[String, String]): Boolean = {
    extracted.getOrElse("SharedAccessKeyName", "") != "" &&
      extracted.getOrElse("SharedAccessKey", "") != ""
  }

  def getOutputClient(connString: String) = {
    val extracted = extractConnectionString(connString)
    // if SharedAccessKey is in connString, build client with SharedAccessKey, else build with RBAC
    if (buildClientFromConnString(extracted)) {
      EventHubClient.createFromConnectionStringSync(connString,executorService)
    } else {
      val endpoint = new URI(extracted.getOrElse("Endpoint", ""))
      val eventHubName = extracted.getOrElse("EntityPath", "")

      val authority = s"https://login.microsoftonline.com/$tenantId"
      val callback = new AuthBySecretCallBack(authority, clientId, clientSecret)

      EventHubClient.createWithAzureActiveDirectory(endpoint, eventHubName, callback, authority, executorService, null).get()
    }
  }

  private class AuthBySecretCallBack(auth: String, clientId: String, clientSecret: String) extends AadAuthenticationCallback {

    implicit def toJavaFunction[A, B](f: Function1[A, B]): function.Function[A, B] = new java.util.function.Function[A, B] {
      override def apply(a: A): B = f(a)
    }

    override def authority: String = auth

    override def acquireToken(audience: String, authority: String, state: Any): CompletableFuture[String] = try {
      val app = ConfidentialClientApplication
        .builder(clientId, ClientCredentialFactory.createFromSecret(clientSecret))
        .authority(authority)
        .build

      val parameters = ClientCredentialParameters.builder(Collections.singleton(audience + ".default")).build

      app.acquireToken(parameters).thenApply((result: IAuthenticationResult) => result.accessToken())
    } catch {
      case e: Exception =>
        val failed = new CompletableFuture[String]
        failed.completeExceptionally(e)
        failed
    }
  }
}
