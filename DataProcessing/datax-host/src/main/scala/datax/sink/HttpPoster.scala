// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.sink

import datax.config.SettingDictionary
import datax.sink.HttpPostOutputSetting.HttpPostConf
import datax.utility.SinkerUtil
import org.apache.http.client.config.RequestConfig
import org.apache.http.client.methods.HttpPost
import org.apache.http.entity.StringEntity
import org.apache.http.impl.client.{BasicResponseHandler, HttpClients}
import org.apache.log4j.LogManager

object HttpPoster extends SinkOperatorFactory {
  val SinkName = "HttpPost"

  def getHttpClient() = {
    val requestConfig = RequestConfig.custom().setConnectionRequestTimeout(5000).setSocketTimeout(5000).build()
    HttpClients.custom().setDefaultRequestConfig(requestConfig).build()
  }

  def postEvents(data: Seq[String], httpEndpoint: String, headers: Option[Map[String, String]], loggerSuffix: String): Int = {
    val logger = LogManager.getLogger(s"HttpPoster${loggerSuffix}")
    val countEvents = data.length
    val chunkSize = 200
    if (countEvents > 0) {
      val clientItr = getHttpClient
      val handler = new BasicResponseHandler

      var i = 0
      data.grouped(chunkSize).foreach(events => {
        val eventsSize = events.length
        val json = "[" + events.mkString(",") + "]"
        val t1 = System.nanoTime()
        val stage = s"[$i-${i + eventsSize}]/$countEvents"

        // post data
        try{
          val post = new HttpPost(httpEndpoint)
          if(headers.isDefined){
            headers.get.foreach(h=>post.addHeader(h._1, h._2))
          }
          post.setEntity(new StringEntity(json))
          val response = clientItr.execute(post)
          val body = handler.handleResponse(response)

          logger.info(s"$stage is sent:${body}")
        }
        catch{
          case e: Exception => {
            logger.error(s"$stage: failed", e)
          }
        }

        i += eventsSize
        eventsSize
      })

      clientItr.close()

      countEvents
    }
    else 0
  }

  def getRowsSinkerGenerator(httpPostConf: HttpPostConf, flagColumnIndex: Int) : SinkDelegate = {
    val sender = (dataToSend:Seq[String],ls: String) => HttpPoster.postEvents(dataToSend, httpPostConf.endpoint, httpPostConf.appendHeaders, ls)
    SinkerUtil.outputGenerator(sender,SinkName)(flagColumnIndex)
  }

  def getSinkOperator(dict: SettingDictionary, name: String) : SinkOperator = {
    val conf = HttpPostOutputSetting.getHttpPostConf(dict, name)
    SinkOperator(
      name = SinkName,
      isEnabled = conf!=null,
      sinkAsJson = true,
      flagColumnExprGenerator = () => conf.filter,
      generator = (flagColumnIndex)=>getRowsSinkerGenerator(conf, flagColumnIndex)
    )
  }

  override def getSettingNamespace(): String = HttpPostOutputSetting.Namespace
}
