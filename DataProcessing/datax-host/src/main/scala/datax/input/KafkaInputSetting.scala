// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.input

import com.microsoft.azure.eventhubs.ConnectionStringBuilder
import datax.config.{SettingDictionary, SettingNamespace}
import datax.exception.EngineException
import datax.securedsetting.KeyVaultClient
import org.apache.log4j.LogManager

// Class to hold kafka config items
case class InputKafkaConf(connectionString: String,
                          bootStrapServers: String,
                          groupId: String,
                          topics: String,
                          isEventHubKafka:Boolean,
                          saslConfig:String,
                          autoOffsetReset: String,
                          sessionTimeout: String,
                          heartBeatInterval: String,
                          fetchMaxWait: String,
                          securityProtocol:String,
                          saslMechanism:String,
                          flushExistingCheckpoints: Option[Boolean],
                          repartition: Option[Int]
                         ) extends InputConf

object KafkaInputSetting extends InputSetting[InputKafkaConf] {


  val NamespaceKafka = "kafka"
  val NamespacePrefix = SettingNamespace.JobInputPrefix + NamespaceKafka+SettingNamespace.Seperator
  val SettingTopics = "topics"
  val SettingKeyDeserializer = "keydeserializer"
  val SettingValueDeserializer = "valuedeserializer"
  val SettingGroupId = "groupid"
  val SettingAutoOffsetReset = "autooffsetreset"
  val SettingRequestTimeout = "requesttimeout"
  val SettingSessionTimeout = "sessiontimeout"
  val SettingHeartbeatInterval = "heartbeatinterval"
  val SettingSecurityProtocol = "securityprotocol"
  val SettingSaslMechanism = "saslmechanism"
  val SettingSaslJaasConfig = "sasljaasconfig"
  val SettingFetchMaxWait = "fetchmaxwait"

  val SettingConnectionString = "connectionstring"

  val SettingCheckpointDir = "checkpointdir"
  val SettingCheckpointInterval = "checkpointinterval"
  val SettingMaxRate = "maxrate"
  val SettingStartEnqueueTime = "startenqueuetime"
  val SettingFlushExistingCheckpoints = "flushexistingcheckpoints"
  val SettingRepartition = "repartition"

  private val logger = LogManager.getLogger("KafkaInputSetting")

  def buildInputKafkaConf(dict: SettingDictionary): InputKafkaConf = {

    val batchInterval = StreamingInputSetting.getStreamingInputConf(dict).intervalInSeconds
    val defaultSessionTimeoutMs = 60001
    val defaultHeartbeatIntervalMs = defaultSessionTimeoutMs/3

    logger.warn("kafka batch interval="+batchInterval.toString)

    val connectionStr = KeyVaultClient.resolveSecretIfAny(dict.get(SettingConnectionString))

    if(connectionStr==null||connectionStr.isEmpty) {
      val errMsg = s"Connection string is empty for kafka input"
      logger.error(errMsg)
      throw new EngineException(errMsg)
    }

    val isEHKafka = isEventHubConnection(connectionStr.get)
    var eh_sasl:String = null
    if(isEHKafka) {
        eh_sasl = "org.apache.kafka.common.security.plain.PlainLoginModule required username=\"$ConnectionString\" password=\"" + connectionStr.get +"\";"
    }


      connectionStr match {
      case Some(connectionString) =>
        InputKafkaConf(
          connectionString = connectionString,
          bootStrapServers = getBootstrapServers(connectionString),
          groupId = dict.getString(SettingGroupId),
          topics = dict.getString(SettingTopics),
          isEventHubKafka=isEHKafka,
          saslConfig = eh_sasl,
          autoOffsetReset = dict.getOrElse(SettingAutoOffsetReset,"latest"),
          sessionTimeout = dict.getOrElse(SettingSessionTimeout, defaultSessionTimeoutMs.toString()),
          heartBeatInterval = dict.getOrElse(SettingHeartbeatInterval, defaultHeartbeatIntervalMs.toString()),
          fetchMaxWait = dict.getOrElse(SettingFetchMaxWait, "10000"),
          securityProtocol = dict.getOrElse(SettingSecurityProtocol,"SASL_SSL"),
          saslMechanism = dict.getOrElse(SettingSaslMechanism,"PLAIN"),
          flushExistingCheckpoints = dict.getBooleanOption(SettingFlushExistingCheckpoints),
          repartition = dict.getIntOption(SettingRepartition)
        )
      case None =>
        null
    }
  }

  def getInputConf(dict: SettingDictionary): InputKafkaConf = {
    logger.warn("Kafka NamespacePrefix=" + NamespacePrefix)
    buildInputKafkaConf(dict.getSubDictionary(NamespacePrefix))
  }

  // Returns bootstrap servers for the kafka input.
  // For kafka enabled eventhub, its the endpoint value with port number.
  private def getBootstrapServers(connectionString:String):String = {

    var bootstrapServers=connectionString

    if(isEventHubConnection(connectionString))
      {
        val endpoint = new ConnectionStringBuilder(connectionString).getEndpoint.toString
        val normalizedEndpoint = getEventHubEndpoint(endpoint)
        bootstrapServers = normalizedEndpoint +":9093"
      }

    logger.warn(s"bootstrapServers: ${bootstrapServers}")

    bootstrapServers
  }

  private def getEventHubEndpoint(connStr:String):String ={

    val regex = """sb?://([\w\d\.]+).*""".r

    regex.findFirstMatchIn(connStr) match {
      case Some(partition) => partition.group(1)
      case None =>  throw new EngineException(s"EventHub connection string does not match the endpoint pattern ${regex.regex}")
    }
  }

  private def isEventHubConnection(connStr:String):Boolean = {
    connStr.startsWith("Endpoint=sb")
  }

  }
