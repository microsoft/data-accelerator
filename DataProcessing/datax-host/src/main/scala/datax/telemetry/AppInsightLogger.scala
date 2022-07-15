// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.telemetry

import com.microsoft.applicationinsights.{TelemetryClient, TelemetryConfiguration}
import datax.config.ConfigManager
import datax.constants.JobArgument
import datax.securedsetting.KeyVaultClient
import datax.service.TelemetryService
import datax.utility.DateTimeUtil
import org.apache.log4j.LogManager
import org.apache.spark.SparkEnv
import org.apache.spark.streaming.Time

import java.sql.Timestamp
import scala.collection.JavaConverters._


object AppInsightLogger extends TelemetryService {
  private val logger = LogManager.getLogger("AppInsightLogger")
  private val config = TelemetryConfiguration.getActive
  private val setDict = ConfigManager.getActiveDictionary()
  setDict.get(JobArgument.ConfName_AppInsightKeyRef) match {
    case Some(keyRef) =>
      KeyVaultClient.getSecret(keyRef) match {
        case Some(key) =>
          config.setInstrumentationKey(key)
          logger.warn("AI Key is set, AppInsight Sender is ON")
        case None =>
          logger.warn(s"AI KeyRef is not found at $keyRef, AppInsight Sender is OFF")
      }

    case None =>
      config.setTrackingIsDisabled(true)
      logger.warn("AI Key is not found, AppInsight Sender is OFF")
  }

  private val client = new TelemetryClient(config)
  private var defaultProps = new scala.collection.mutable.HashMap[String, String]

  private def addContextProps(properties: Map[String, String]) = {
    defaultProps ++= properties.map(k=>("context."+k._1)->k._2)
    logger.info(s"add context properties:$properties")
    logger.info(s"Current context properties:$defaultProps")
  }

  def trackEvent(event: String) = {
    client.trackEvent(event, mergeProps(null), mergeMeasures(null))
  }

  def trackException(e: Exception) = {
    client.trackException(e, mergeProps(null), mergeMeasures(null))
  }

  def mergeProps(props: Map[String, String]):java.util.Map[java.lang.String, java.lang.String] = {
    if(props == null){
      if(defaultProps.size==0)
        null
      else
        defaultProps.map(p=>(p._1, p._2)).asJava
    }
    else{
      (defaultProps++props).map(p=>(p._1, p._2)).asJava
    }
  }

  def mergeMeasures(measures: Map[String, Double]):java.util.Map[java.lang.String, java.lang.Double] = {
    if(measures==null)
      null
    else
      measures.map(p=>(p._1, Double.box(p._2))).asJava
  }

  def trackEvent(event: String, properties: Map[String, String], measurements: Map[String, Double]) = {
    logger.warn(s"sending event: $event")
    client.trackEvent(event, mergeProps(properties), mergeMeasures(measurements))
  }

  def trackBatchEvent(event: String, properties: Map[String, String], measurements: Map[String, Double], batchTime: Timestamp): Unit = {
    //val batchTimeStr = DateTimeUtil.formatSimple(batchTime)
    val batchTimeStr = batchTime.toString
    trackEvent(event, Option(properties).map(x => x ++ Map("context.batchtime" -> batchTimeStr)).getOrElse(Map("context.batchtime" -> batchTimeStr)), measurements)
  }

  def trackBatchEvent(event: String, properties: Map[String, String], measurements: Map[String, Double], batchTime: Time): Unit = {
    trackBatchEvent(event, properties, measurements, new Timestamp(batchTime.milliseconds))
  }

  def trackException(e: Exception, properties: Map[String, String], measurements: Map[String, Double]) = {
    logger.warn(s"sending exception: ${e.getMessage}")
    client.trackException(e, mergeProps(properties), mergeMeasures(measurements))
  }

  def initForApp(appName: String) = {
    val sparkEnv = SparkEnv.get

    val props =
      if(sparkEnv==null){
        Map(
          "appname" -> appName,
          "appid" -> "SparkEnv not initlialized",
          "executorid" -> "Unknown"
        )
      }
      else{
        Map(
          "appname" -> appName,
          "appid" -> sparkEnv.conf.getAppId,
          "executorid" -> sparkEnv.executorId
        )
      }

    logger.warn(s"Initialize AppInsightLogger context props:"+props.toString())
    addContextProps(props)
  }

  initForApp(setDict.getAppName())
}
