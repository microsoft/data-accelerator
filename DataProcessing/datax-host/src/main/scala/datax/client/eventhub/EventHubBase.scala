// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.client.eventhub

import java.util.concurrent.Executors

import com.microsoft.azure.eventhubs.EventHubClient
import org.apache.spark.eventhubs.ConnectionStringBuilder

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

  def getOutputClient(connString: String) = {
    EventHubClient.createFromConnectionStringSync(connString,executorService)
  }
}
