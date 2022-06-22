// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.telemetry

import datax.config.SparkEnvVariables
import datax.handler.MetricSinkConf
import datax.client.eventhub.{EventHubBase, EventHubSender}
import datax.client.redis.{RedisBase, RedisConnection}
import datax.constants.ProductConstant
import datax.sink.HttpPoster
import org.apache.log4j.LogManager

class MetricLogger(name: String, redis: RedisConnection, eventhub: EventHubSender, httpEndpoint:String) {
  private val logger = LogManager.getLogger("MetricLogger-"+name)
  private val sendEventhub = eventhub!=null
  private val sendRedis = redis!=null
  private val sendHttp = httpEndpoint!=null

  def putMetricOnRedis(key: String, ts: Long, value: String) = {
    redis.synchronized{
      redis.getSortedSetCommands.zadd(key, ts, value)
    }
  }

  def sendMetric(metricName: String, timestamp: Long, value: Double): Unit = {
    if(sendRedis || sendEventhub || sendHttp) {
      val t1 = System.nanoTime()

      val json =
        s"""{"uts":$timestamp, "val":$value}"""
      val ts = new java.util.Date().getTime
      if (sendRedis) putMetricOnRedis(name + ":" + metricName, ts, json)

      val fullJson = s"""{"app":"$name", "met":"$metricName","uts":$timestamp, "val":$value}"""
      if (sendEventhub) eventhub.sendString(fullJson, null)

      if(sendHttp){
        val header = Map("Content-Type" -> "application/json")
        HttpPoster.postEvents(Seq(fullJson), httpEndpoint, Some(header),"metricLogger")
      }

      AppInsightLogger.trackEvent("BatchMetric", Map("timestamp" -> timestamp.toString), Map(metricName -> value))

      val elapsedTime = (System.nanoTime() - t1) / 1E9
      logger.warn(s"done sending metric for $metricName in $elapsedTime seconds, redis:$sendRedis, eventhub:$sendEventhub")
    }
    else{
      logger.warn(s"skipped sending metric for $metricName as no metrics output are configured")
    }
  }

  def sendBatchMetrics(metrics: Iterable[(String, Double)], timestamp: Long): Unit = {
    if(sendRedis || sendEventhub || sendHttp){
      val t1=System.nanoTime()
      val ts = new java.util.Date().getTime

      if(sendRedis)metrics.foreach{m=>putMetricOnRedis(name+":"+m._1, ts, s"""{"uts":$timestamp, "val":${m._2}}""")}

      if(sendEventhub) {
        val fullJson = metrics.map { case (k, v) => s"""{"app":"$name", "met":"$k","uts":$timestamp, "val":$v}""" }.mkString("\n")
        eventhub.sendString(fullJson, null)
      }

      if(sendHttp) {
        val fullJson = metrics.map { case (k, v) => s"""{"app":"$name", "met":"$k","uts":$timestamp, "val":$v}""" }
        val header = Map("Content-Type" -> "application/json")
        HttpPoster.postEvents((fullJson).toSeq, httpEndpoint, Some(header),"metricLogger")
      }

      AppInsightLogger.trackEvent(ProductConstant.ProductRoot + "/BatchMetrics", Map("timestamp" -> timestamp.toString), metrics.toMap)

      val elapsedTime = (System.nanoTime()-t1)/1E9
      logger.warn(s"done sending batch metrics(${metrics.size}) in $elapsedTime seconds, redis:$sendRedis, eventhub:$sendEventhub")
    }
    else{
      logger.warn(s"skipped sending batch metrics(${metrics.size}) as no metrics output are configured")
    }
  }
}

object MetricLoggerFactory {
  var sender: MetricLogger = null
  def getOrCreateLogger(parametersGenerator: ()=>(String, MetricSinkConf, String)): MetricLogger = {
    if(sender == null){
      this.synchronized {
        if(sender == null){
          val (appName, sink, clientName) = parametersGenerator()
          sender = new MetricLogger(appName,
            if(sink.redis==null) null else RedisBase.getConnection(sink.redis, clientName),
            if(sink.eventhub==null) null else new EventHubSender(EventHubBase.getOutputClient(sink.eventhub)),
            if(sink.httpEndpoint==null) null else sink.httpEndpoint
          )
        }
      }
    }

    sender
  }

  def getMetricLogger(appName: String, metricConf: MetricSinkConf) = {
    getOrCreateLogger(()=>(appName, metricConf, SparkEnvVariables.getClientNodeName(appName)))
  }
}
