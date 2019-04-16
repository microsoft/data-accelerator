// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.handler

import datax.config.{SettingDictionary, SettingNamespace}
import datax.securedsetting.KeyVaultClient
import org.apache.log4j.LogManager

case class MetricSinkConf(redis: String, eventhub: String, httpEndpoint:String)

object MetricsHandler {
  val Namespace = "metric"
  val NamespacePrefix = SettingNamespace.JobProcessPrefix+Namespace+SettingNamespace.Seperator
  val SettingRedisConnection= "redis"
  val SettingEventHubConnection= "eventhub"
  val SettingHttpConnection= "httppost"

  val logger = LogManager.getLogger(this.getClass)

  def getMetricsSinkConf(dict: SettingDictionary):MetricSinkConf = {
    val prefix = NamespacePrefix
    val subdict = dict.getSubDictionary(prefix)

    MetricSinkConf(
      redis = KeyVaultClient.resolveSecretIfAny(subdict.getOrNull(SettingRedisConnection)),
      eventhub = KeyVaultClient.resolveSecretIfAny(subdict.getOrNull(SettingEventHubConnection)),
      httpEndpoint = subdict.getOrNull(SettingHttpConnection)
    )
  }

}
