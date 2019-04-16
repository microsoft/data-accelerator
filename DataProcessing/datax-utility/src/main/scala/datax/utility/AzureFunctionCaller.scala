// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.utility

import datax.constants.ProductConstant
import java.util.concurrent.{ConcurrentLinkedQueue, Semaphore}

import org.apache.http.client.methods.HttpGet
import org.apache.http.impl.client.HttpClients
import org.apache.http.util.EntityUtils
import org.apache.log4j.LogManager

class AzureFunctionCaller {
  val client = HttpClients.createDefault()
  val logger = LogManager.getLogger("AzureFunctionCaller")

  def callWithRetries(request: HttpGet, retries: Int): String = {
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

  def call(host: String, api: String, code: String, params: Map[String, String]):String = {
    val paramsExpr = if(params!=null && params.size>0){
      params.map(p=>p._1+"="+p._2).mkString("&")
    }
    else null

    val codeExpr = if(code==null)null else "code="+code
    val query = if(paramsExpr==null){
      if(codeExpr==null)"" else "?"+codeExpr
    }
    else{
      if(codeExpr==null)s"?$paramsExpr" else s"?$codeExpr&$paramsExpr"
    }

    val request = new HttpGet(s"https://$host.azurewebsites.net/api/$api/$query")
    request.addHeader("Content-Type", "application/json")
    callWithRetries(request, 5)
  }
}


object AzureFunctionCaller {
  val MaxClientCount = 20
  val sem = new Semaphore(MaxClientCount)
  val pool = new ConcurrentLinkedQueue[AzureFunctionCaller]()

  def call(host: String, api: String, code: String, params: Map[String, String]): String = {
    sem.acquire()
    val caller = Option(pool.poll()).getOrElse(new AzureFunctionCaller)
    val result = caller.call(host, api, code, params)
    pool.add(caller)
    sem.release()

    result
  }

  def callHelloWorldFunc(name: String) = {
    call(ProductConstant.ProductRoot + "-helloworldfunc", "hello", null, Map("name"->name))
  }
}
