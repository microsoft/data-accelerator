// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.telemetry

import com.microsoft.applicationinsights.extensibility.ContextInitializer
import com.microsoft.applicationinsights.log4j.v1_2.ApplicationInsightsAppender
import com.microsoft.applicationinsights.telemetry.TelemetryContext
import com.microsoft.applicationinsights.{TelemetryClient, TelemetryConfiguration}
import datax.config.ConfigManager
import datax.constants.JobArgument
import datax.securedsetting.KeyVaultClient
import datax.service.TelemetryService
import datax.utility.DateTimeUtil
import org.apache.log4j.{Level, LogManager, PatternLayout}
import org.apache.spark.SparkEnv
import org.apache.spark.streaming.Time

import java.sql.Timestamp
import scala.collection.JavaConverters._


object AppInsightLogger extends TelemetryService {
  private val logger = LogManager.getLogger("AppInsightLogger")
  private val config = TelemetryConfiguration.getActive
  private val setDict = ConfigManager.getActiveDictionary()
  private val aiAppender = new ApplicationInsightsAppender()
  setDict.get(JobArgument.ConfName_AppInsightKeyRef) match {
    case Some(keyRef) =>
      KeyVaultClient.getSecret(keyRef) match {
        case Some(key) =>
          config.setInstrumentationKey(key)
          aiAppender.setInstrumentationKey(key)
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
  private var batchMetricProps = new scala.collection.mutable.HashMap[String, String]

  def IsEnabled() = !client.isDisabled

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

  def trackMetric(event: String, properties: Map[String, String]) = {
    logger.warn(s"sending metric: $event")
    client.trackMetric(event, 0, 1, 0, 0, 0, (batchMetricProps ++ properties).asJava)
  }

  def trackBatchMetric(event: String, properties: Map[String, String], batchTime: Timestamp) = {
    val batchTimeStr = Option(batchTime).map(_.toString).getOrElse("")
    val batchTimeMetricProp = Map("Batch date" -> batchTimeStr)
    trackMetric(event, Option(properties).map(x => x ++ batchTimeMetricProp).getOrElse(batchTimeMetricProp))
  }

  def trackBatchEvent(event: String, properties: Map[String, String], measurements: Map[String, Double], batchTime: Timestamp): Unit = {
    val batchTimeStr = Option(batchTime).map(_.toString).getOrElse("")
    val batchTimeMetricProp = Map("context.batchtime" -> batchTimeStr) ++ Map("Batch date" -> batchTimeStr)
    trackEvent(event, Option(properties).map(x => x ++ batchTimeMetricProp).getOrElse(batchTimeMetricProp), measurements)
  }

  def trackBatchEvent(event: String, properties: Map[String, String], measurements: Map[String, Double], batchTime: Time): Unit = {
    trackBatchEvent(event, properties, measurements, new Timestamp(batchTime.milliseconds))
  }

  def trackException(e: Exception, properties: Map[String, String], measurements: Map[String, Double]) = {
    logger.warn(s"sending exception: ${e.getMessage}")
    client.trackException(e, mergeProps(properties), mergeMeasures(measurements))
  }

  def initForApp(appName: String, mode: String = "") = {
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
    batchMetricProps ++= Map(
      "Pipeline name" -> appName,
      "Pipeline mode" -> mode,
      "Pipeline component" -> "HDInsight"
    )
  }

  initForApp(setDict.getAppName())

  if(IsEnabled()) {
    // Enable Log4j appender that uses Application insights
    aiAppender.activateOptions()
    aiAppender.setThreshold(Level.ERROR)
    aiAppender.getTelemetryClientProxy.getTelemetryClient.getContext.getProperties.putAll(batchMetricProps.asJava)
    LogManager.getRootLogger.addAppender(aiAppender)
  }
}
