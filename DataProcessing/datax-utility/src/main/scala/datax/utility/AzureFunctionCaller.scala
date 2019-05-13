// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.utility

import datax.constants.ProductConstant
import java.util.concurrent.{ConcurrentLinkedQueue, Semaphore}
import org.apache.http.client.methods.{HttpGet, HttpPost, HttpRequestBase}
import org.apache.http.entity.StringEntity
import org.apache.http.impl.client.HttpClients
import org.apache.http.util.EntityUtils
import org.apache.log4j.LogManager

import scala.util.parsing.json.JSONObject

class AzureFunctionCaller {
  val client = HttpClients.createDefault()
  val logger = LogManager.getLogger("AzureFunctionCaller")

  def callWithRetries(request: HttpRequestBase, retries: Int): String = {
    try{
      val response = client.execute(request)
      val result = EntityUtils.toString(response.getEntity())
      result
    }
    catch {
      case e: Exception =>
        if(retries>0){
          logger.warn(s"Error from request: ${request.getURI} with message '${e.getMessage}', remaining retries:${retries-1}")
          callWithRetries(request, retries-1)
        }
        else{
          logger.error(s"Error from request: ${request.getURI}, throw since no retries left", e)
          throw e
        }
    }
  }

  // Make a request to the AzureFunction based on the method type
  def call(host: String, api: String, code: String, methodType:String, params: Map[String, String]):String = {

    if (methodType == "get") {
      val query = getQueryParams(code, params)
      val request = new HttpGet(s"https://$host.azurewebsites.net/api/$api/$query")
      request.addHeader("Content-Type", "application/json")
      callWithRetries(request, 5)
    }
    else if (methodType == "post") {
      // For POST, params is passed as msg body, hence send only code if available
      val query = getQueryParams(code, Map.empty[String,String])
      val request = new HttpPost(s"https://$host.azurewebsites.net/api/$api/$query")
      request.addHeader("Content-Type", "application/json")
      val data = JSONObject(params).toString()
      request.setEntity(new StringEntity(data))
      callWithRetries(request, 5)
    }
    else {
      throw new Exception(s"AzureFuction method type $methodType not supported")
    }
  }

  // Construct query params for the Azure Function including the code (function key)
  private def getQueryParams(code:String, params: Map[String, String]):String = {

    val paramsExpr = if (params != null && params.size > 0) {
      params.map(p => p._1 + "=" + p._2).mkString("&")
    }
    else null

    val codeExpr = if (code == null) null else "code=" + code
    val query = if (paramsExpr == null) {
      if (codeExpr == null) "" else "?" + codeExpr
    }
    else {
      if (codeExpr == null) s"?$paramsExpr" else s"?$codeExpr&$paramsExpr"
    }

    query
  }
}


object AzureFunctionCaller {
  val MaxClientCount = 20
  val sem = new Semaphore(MaxClientCount)
  val pool = new ConcurrentLinkedQueue[AzureFunctionCaller]()

  def call(host: String, api: String, code: String, methodType:String, params: Map[String, String]): String = {
    sem.acquire()
    val caller = Option(pool.poll()).getOrElse(new AzureFunctionCaller)
    val result = caller.call(host, api, code, methodType, params)
    pool.add(caller)
    sem.release()

    result
  }
}
