// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.input
import datax.config.{SettingDictionary,SettingNamespace}
import org.apache.log4j.LogManager

case class InputEventHubConf(connectionString: String,
                             consumerGroup: String,
                             checkpointDir: String,
                             checkpointInterval: String,
                             maxRate: String,
                             startEnqueueTime: Option[Long],
                             flushExistingCheckpoints: Option[Boolean],
                             repartition: Option[Int]
                            ) extends InputConf


object EventHubInputSetting extends InputSetting[InputEventHubConf] {

  val NamespaceEventHub = "eventhub"
  val NamespacePrefix = SettingNamespace.JobInputPrefix + NamespaceEventHub+SettingNamespace.Seperator
  val SettingConnectionString = "connectionstring"
  val SettingConsumerGroup = "consumergroup"
  val SettingCheckpointDir = "checkpointdir"
  val SettingCheckpointInterval = "checkpointinterval"
  val SettingMaxRate = "maxrate"
  val SettingStartEnqueueTime = "startenqueuetime"
  val SettingFlushExistingCheckpoints = "flushexistingcheckpoints"
  val SettingRepartition = "repartition"

  private val logger = LogManager.getLogger("EventHubInputSetting")

  def buildInputEventHubConf(dict: SettingDictionary): InputEventHubConf = {
    dict.get(SettingConnectionString) match {
      case Some(connectionString) =>
        InputEventHubConf(
          connectionString = connectionString,
          consumerGroup = dict.getString(SettingConsumerGroup),
          checkpointDir = dict.getString(SettingCheckpointDir),
          checkpointInterval = dict.getString(SettingCheckpointInterval),
          maxRate = dict.getString(SettingMaxRate),
          startEnqueueTime = dict.getLongOption(SettingStartEnqueueTime),
          flushExistingCheckpoints = dict.getBooleanOption(SettingFlushExistingCheckpoints),
          repartition = dict.getIntOption(SettingRepartition)
        )
      case None =>
        null
    }
  }

  def getInputConf(dict: SettingDictionary): InputEventHubConf = {
    logger.warn("EventHub NamespacePrefix=" + NamespacePrefix)
    buildInputEventHubConf(dict.getSubDictionary(NamespacePrefix))
  }
}
