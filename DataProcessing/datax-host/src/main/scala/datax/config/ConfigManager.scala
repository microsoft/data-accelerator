// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.config

import datax.constants.JobArgument
import datax.exception.EngineException
import datax.fs.HadoopClient
import datax.service.ConfigService
import datax.utility.ArgumentsParser
import org.apache.commons.lang3.StringUtils
import org.apache.log4j.LogManager
import org.apache.spark.SparkConf

/***
  * Singleton service to set and get a Dictionary object
  */
object ConfigManager extends ConfigService {
  private val logger = LogManager.getLogger("DictionaryigManager")
  @transient private var sparkConf: SparkConf = _

  def getSparkConf() = sparkConf

  def initSparkConf(inputArguments: Array[String]) = {
    val namedArgs = ArgumentsParser.getNamedArgs(inputArguments)
    sparkConf = new SparkConf()
    namedArgs.foreach(arg => {
      if (StringUtils.isNotEmpty(arg._2) && StringUtils.isNotEmpty(arg._1) && arg._1.startsWith("spark.")) {
        val property = arg._1
        val value = arg._2
        sparkConf.set(property, value)
      }
    })
    sparkConf
  }

  def loadLocalConfigIfExists[T](configurationFile: String)(implicit mf: Manifest[T]) : Option[T] = {
    val configString = HadoopClient.readLocalFileIfExists(configurationFile)

    if(configString==null)
      None
    else {
      implicit val formats = org.json4s.DefaultFormats
      Some(org.json4s.jackson.parseJson(configString).extract[T])
    }
  }

  private def getLocalEnvVars(): Map[String, String] = {
    sys.env.filterKeys(_.startsWith(JobArgument.ConfNamePrefix))
  }

  private def getLocalConf(): SettingDictionary = {
    SettingDictionary(getLocalEnvVars())
  }

  var singletonConfDict: SettingDictionary = null
  def getActiveDictionary(): SettingDictionary = {
    if(singletonConfDict==null){
      ConfigManager.synchronized{
        if(singletonConfDict==null){
          singletonConfDict = getLocalConf()
        }
      }
    }

    singletonConfDict
  }

  def setActiveDictionary(conf: SettingDictionary) = {
    ConfigManager.synchronized{
      singletonConfDict = conf
    }
  }

  def getConfigurationFromArguments(inputArguments: Array[String]):SettingDictionary = {
    val namedArgs = ArgumentsParser.getNamedArgs(inputArguments)

    logger.warn("cmd line args:"+namedArgs)

    if (!namedArgs.contains("conf")) {
      throw new Error("configuration file is not specified.")
    }

    val envs = getLocalEnvVars()
    val convertedConf = Map(
      JobArgument.ConfName_AppConf -> namedArgs.getOrElse("conf", null),
      JobArgument.ConfName_DriverLogLevel -> namedArgs.getOrElse("driverLogLevel", null),
      JobArgument.ConfName_LogLevel -> namedArgs.getOrElse("executorLogLevel", null),
      JobArgument.ConfName_CheckpointEnabled -> namedArgs.getOrElse("checkpointEnabled", null),
      "partition" -> namedArgs.getOrElse("partition", null),
      "trackerFolder" -> namedArgs.getOrElse("trackerFolder", null)
    ).filter(_._2!=null)

    logger.warn("local env:"+envs.mkString(","))
    setActiveDictionary(SettingDictionary(envs ++ namedArgs ++ convertedConf))
    singletonConfDict
  }

  private def replaceTokens(src: String, tokens: Map[String, String]) = {
    if(tokens==null || src==null ||src.isEmpty)
      src
    else
      tokens.foldLeft(src)((r, kv) => r.replaceAllLiterally("${" + kv._1 + "}", kv._2))
  }

  private def readJsonFile[T](configurationFile: String, replacements: Map[String, String])(implicit mf: Manifest[T]): T = {
    val configString = HadoopClient.readHdfsFile(configurationFile).mkString("")
    implicit val formats = org.json4s.DefaultFormats
    org.json4s.jackson
      .parseJson(replaceTokens(configString, replacements))
      .extract[T]
  }

  private def splitString(s: String):(String, String) = {
    val pos = s.indexOf("=")
    if(pos==0)
      ""->s
    else if(pos>0)
      s.substring(0, pos).trim()->s.substring(pos+1).trim()
    else
      s.trim()->null
  }

  private def readConfFile(filePath: String, replacements: Map[String, String]) = {
    if(filePath==null)
      throw new EngineException(s"No conf file is provided")
    else if(!filePath.toLowerCase().endsWith(".conf"))
      throw new EngineException(s"non-conf file is not supported as configuration input")

    parseConfLines(HadoopClient.readHdfsFile(filePath), replacements)
  }

  def loadConfig(sparkConf: SparkConf): UnifiedConfig = {
    val dict = getActiveDictionary()
    val confFile = dict.getAppConfigurationFile()
    val confProps = readConfFile(confFile, dict.dict)
    val newdict = SettingDictionary(dict.dict ++ confProps)
    setActiveDictionary(newdict)

    logger.warn("Load Dictionary as following:\n"+newdict.dict.map(kv=>s"${kv._1}->${kv._2}").mkString("\n"))
    UnifiedConfig(sparkConf = sparkConf, dict = newdict)
  }

 private def parseConfLines(lines: Iterable[String], replacements: Map[String, String]) = {
    lines
      // skip empty lines or commented lines
      .filter(l=>l!=null && !l.trim().isEmpty && !l.trim().startsWith("#"))
      .map(splitString)
      .map{case(k,v)=>k->replaceTokens(v, replacements)}
      .toMap
  }
}
