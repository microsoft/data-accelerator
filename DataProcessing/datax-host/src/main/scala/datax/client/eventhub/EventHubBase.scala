// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.client.eventhub

import java.net.URI
import java.time.Duration
import java.util.concurrent.{CompletableFuture, Executors}
import com.microsoft.azure.eventhubs.{EventHubClient, ITokenProvider, JsonSecurityToken, SecurityToken}
import org.apache.spark.eventhubs.ConnectionStringBuilder
import scala.compat.java8.FunctionConverters._
import datax.authentication.ManagedIdentity

object EventHubBase {
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
      EventHubClient.createWithTokenProvider(endpoint, eventHubName, new ManagedIdentityTokenProvider(), executorService, null).get()
    }
  }

  private class ManagedIdentityTokenProvider extends ITokenProvider {
    override def getToken(resource: String, timeout: Duration): CompletableFuture[SecurityToken] = {
      val resourceId = "https://eventhubs.azure.net"
      val tokenStr = ManagedIdentity.getAccessToken(resourceId)
      val supplier = () => {
        new JsonSecurityToken(tokenStr, resourceId).asInstanceOf[SecurityToken]
      }

      CompletableFuture.supplyAsync(supplier.asJava)
    }
  }
}
