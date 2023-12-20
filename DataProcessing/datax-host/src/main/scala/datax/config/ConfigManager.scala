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

import scala.util.Try

/***
  * Singleton service to set and get a Dictionary object
  */
object ConfigManager extends ConfigService {
  private val logger = LogManager.getLogger("DictionaryigManager")
  @transient private var sparkConf: SparkConf = _

  /**
   * Retrieves the current spark configuration singleton
   * @return
   */
  def getSparkConf() = sparkConf

  /**
   * Forwards any spark configuration property from input arguments to the current spark configuration object
   * @param inputArguments The array of input arguments
   * @return
   */
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

  /**
   * Auxiliary method to decode a string base64 value into its original representation
   * @param base64str A base64 encoded string
   * @return
   */
  def decodeBase64(base64str: String) : Option[String] = {
    Try(new String(java.util.Base64.getDecoder.decode(base64str))).toOption
  }

  /**
   * Attempts to decode a configuration file path as content supplied as a base64 encoded string
   * @param fileContents The raw file contents encoded as base64
   * @return
   */
  def readConfigFileAsBase64Encoded(fileContents: String): Option[Iterable[String]] = {
    decodeBase64(fileContents)
      .map(contents => contents.replace("\r", "").trim().split("\n"))
  }

  /**
   * Attempts to load the file from the path provided
   * @param filePath The file path
   * @param extension The extension the file must have
   * @return
   */
  def readConfigFileAsFile(filePath: String, extension: Option[String] = None): Iterable[String] = {
    if (extension.isDefined && !filePath.toLowerCase().endsWith(extension.get))
      throw EngineException(s"non-conf file is not supported as configuration input")
    HadoopClient.readHdfsFile(filePath)
  }

  /**
   * Attempts to load the configuration file considering the file path as a base64 encoded content first, if
   * that fails, load the file from the file path
   * @param filePath The base64 encoded file content or a file path
   * @param extension The extension the file path must have (if filePath is not content)
   * @return
   */
  def loadConfigFile(filePath: String, extension: Option[String] = None): Iterable[String] = {
    // Try to decode any base64 encode config first, then fallback to look it as a filepath
    readConfigFileAsBase64Encoded(filePath)
      .getOrElse(readConfigFileAsFile(filePath, extension))
  }

  /**
   * Attempts to load the configuration file considering the file path as a base64 encoded content first, if
   * that fails, load the file from the file path
   * @param filePath The base64 encoded file content or a file path
   * @param replacements Substitutions to be made on the file content
   * @return
   */
  private def readConfFile(filePath: String, replacements: Map[String, String]) = {
    if(filePath==null)
      throw EngineException(s"No conf file or conf data is provided")

    parseConfLines(loadConfigFile(filePath, Option(".conf")), replacements)
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
