// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.config

import datax.constants.{JobArgument, ProductConstant}
import datax.exception.EngineException
import org.apache.log4j.Level

import scala.concurrent.duration.Duration


/***
  * represents configuration of a job
  *
  * @param elems starting elements in the configuration
  * @param parentPrefix prefix of all of the setting names in this dictionary, mainly for logging
  */
case class SettingDictionary(elems: Map[String, String], parentPrefix: String = SettingNamespace.DefaultSettingName){
  val dict = elems
  val size = dict.size

  def getDictMap() = dict
  def get(key: String) = dict.get(key)
  def getOrNull(key: String) = get(key).orNull
  def getDefault() = get(SettingNamespace.DefaultSettingName)

  def getOrThrow[T](opt: Option[T], key: String) = {
    opt match {
      case Some(v)=> v
      case None => throw new EngineException(s"config setting '${parentPrefix+key}' is not found")
    }
  }

  def getString(key: String) = getOrThrow(dict.get(key), key)
  def getOrElse(key: String, defaultValue: String) = dict.getOrElse(key, defaultValue)
  def getIntOption(key: String) = get(key).map(_.toInt)
  def getLongOption(key: String) = get(key).map(_.toLong)
  def getLong(key: String) = getOrThrow(getLongOption(key), key)

  def getDoubleOption(key: String) = get(key).map(_.toDouble)
  def getDouble(key: String) = getOrThrow(getDoubleOption(key), key)
  def getBooleanOption(key: String) = get(key).map(_.toBoolean)
  def getDurationOption(key: String) = get(key).map(Duration.create(_))
  def getDuration(key: String) = getOrThrow(getDurationOption(key), key)

  def getStringSeqOption(key: String) = get(key).map(str => {
    val seq = str.split(SettingNamespace.ValueSeperator).filterNot(_.isEmpty).toSeq
    if (seq.length > 0) seq else null
  })

  private def findWithPrefix(prefix: String): Map[String, String] = dict.filter(kv=>kv._1.startsWith(prefix))

  private def stripKeys(dict: Map[String, String], startIndex: Int) = {
    dict.filter(kv=>kv._1!=null&&kv._1.length>startIndex).map{case(k, v) => k.substring(startIndex)->v}
  }

  private def stripKeysByNamespace(dict: Map[String, String], namespace: String) = {
    val prefixLength = (namespace+SettingNamespace.Seperator).length
    dict.filter(kv=>kv._1!=null&&kv._1.length>=namespace.length).map{case(k, v) => {
      if(k==namespace)
        SettingNamespace.DefaultSettingName -> v
      else
        k.substring(prefixLength)->v
    }}
  }

  /** group the dictionary into sub dictionary based on namespaces
    *
    * strip off the prefix on every setting names, and distribute settings into groups of [[SettingDictionary]] with the group name
    * is the first namespace in the setting name.
    *
    * @param prefix the prefix to look for and strip off when grouping
    * @return a group of SettingDictionary
    */
  def groupBySubNamespace(prefix: String = null) = {
    val sub = if(prefix==null || prefix.isEmpty)
      dict
    else
      stripKeys(findWithPrefix(prefix), prefix.length)

    sub.groupBy(kv=>SettingNamespace.getSubNamespace(kv._1, 0))
      .filterKeys(_!=null)
      .map{case (k, v) => k-> SettingDictionary(stripKeysByNamespace(v, k), parentPrefix + k + SettingNamespace.Seperator)}
  }

  /** get a sub [[SettingDictionary]] with only setting whose names start with the give prefix
    *
    * @param prefix prefix to filter the setting name
    * @return a [[SettingDictionary]] instance containing only the settings with prefix in the name
    */
  def getSubDictionary(prefix: String) = {
    SettingDictionary(stripKeys(findWithPrefix(prefix), prefix.length), parentPrefix+prefix)
  }

  def buildConfigIterable[TConf](builder: (SettingDictionary, String)=>TConf, prefix: String = null) = {
    groupBySubNamespace(prefix)
      .map{case(k,v)=>builder(v, k)}
  }

  def buildConfigMap[TConf](builder: (SettingDictionary, String)=>TConf, prefix: String = null) = {
    groupBySubNamespace(prefix)
      .map{case(k,v)=>k->builder(v, k)}
  }

  /***
    * get name of the job
    * @return name of the job
    */
  def getAppName(): String = {
    dict.getOrElse(JobArgument.ConfName_AppName, DefaultValue.DefaultAppName)
  }

  def getJobName(): String = {
    dict.get(SettingNamespace.JobNameFullPath).getOrElse(getAppName())
  }

  def getMetricAppName() = {
    ProductConstant.MetricAppNamePrefix + getJobName()
  }

  def getClientNodeName() = {
    SparkEnvVariables.getClientNodeName(this.getAppName())
  }

  /***
    * get path to configuration file of the job
    * @return path to the configuration file
    */
  def getAppConfigurationFile(): String = {
    dict.getOrElse(JobArgument.ConfName_AppConf, null)
  }

  /***
    * get setting of logging level on driver nodes of the job
    * @return logging level on driver nodes
    */
  def getDriverLogLevel(): Option[Level] = {
    dict.get(JobArgument.ConfName_DriverLogLevel).map(Level.toLevel(_))
  }

  /***
    * get setting of logging level on executor nodes of the job
    * @return logging level on executor nodes
    */
  def getExecutorLogLevel(): Option[Level] = {
    dict.get(JobArgument.ConfName_LogLevel).map(Level.toLevel(_))
  }
}

