//// *********************************************************************
//// Copyright (c) Microsoft Corporation.  All rights reserved.
//// Licensed under the MIT License
//// *********************************************************************
//package datax.keyvault
//
//import org.apache.http.client.config.RequestConfig
//import org.apache.http.client.methods.HttpGet
//import org.apache.http.impl.client.{BasicResponseHandler, HttpClients}
//
//object HttpGetter {
//
//  private def timeOutValue = 5000 // 5s
//
//  def getHttpClient() = {
//    val requestConfig = RequestConfig.custom().setConnectionRequestTimeout(timeOutValue).setSocketTimeout(timeOutValue).build()
//    HttpClients.custom().setDefaultRequestConfig(requestConfig).build()
//  }
//
//// This is used to call local msi endpoint
//  def httpGet(httpEndpoint: String, headers: Option[Map[String,String]]): String = {
//
//    val clientItr = getHttpClient
//    val handler = new BasicResponseHandler
//
//    var result = ""
//
//    try {
//      val get = new HttpGet(httpEndpoint)
//
//      if(headers.isDefined){
//        headers.get.foreach(h=>get.addHeader(h._1, h._2))
//      }
//
//      val response = clientItr.execute(get)
//      result = handler.handleResponse(response)
//    }
//    finally {
//      clientItr.close()
//    }
//    result
//  }
//}
