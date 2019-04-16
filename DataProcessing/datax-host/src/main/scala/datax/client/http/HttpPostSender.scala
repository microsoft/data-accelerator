// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.client.http

import org.apache.http.client.config.RequestConfig
import org.apache.http.client.methods.{CloseableHttpResponse, HttpPost}
import org.apache.http.entity.StringEntity
import org.apache.http.impl.client.{CloseableHttpClient, HttpClientBuilder}
import org.apache.http.util.EntityUtils
import org.apache.log4j.LogManager

class HttpPostSender(url: String) {
  def createClient(): CloseableHttpClient ={
    val timeout = 5*1000
    val requestConfig = RequestConfig.custom()
                              .setConnectionRequestTimeout(timeout)
                              .setConnectTimeout(timeout)
                              .setSocketTimeout(timeout)
      .build()
    HttpClientBuilder.create().setDefaultRequestConfig(requestConfig).build()
  }

  private val client = createClient()
  private val poster = new HttpPost(url)
  poster.addHeader("Content-Type", "application/json")

  protected val senderName: String = this.getClass.getSimpleName

  val logger = LogManager.getLogger(this.getClass)
  logger.info(s"Constructing HttpPoster '$senderName' with url '$url'")

  def sendString(data: String): String = {
    val t1 = System.nanoTime()
    val result = this.synchronized{
      poster.setEntity(new StringEntity(data))
      var response: CloseableHttpResponse = null
      try {
        response = client.execute(poster)
        EntityUtils.toString(response.getEntity())
      }
      catch {
        case e: Exception =>
          val msg = s"!!HttpPoster failed to send '$data' to '$url'"
          logger.error(msg, e)
          msg
      }
      finally {
        if(response!=null){
          response.close()
          response=null
        }
      }
    }

    val time = System.nanoTime()-t1
    logger.warn(s"HttpPoster Result:'$result' within ${time/1E9} seconds")
    result
  }
}


