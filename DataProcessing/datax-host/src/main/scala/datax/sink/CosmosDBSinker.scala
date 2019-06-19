// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.sink

import java.util.concurrent.ConcurrentHashMap

import com.microsoft.azure.documentdb._
import datax.client.cosmosdb.CosmosDBConf
import datax.config.SettingDictionary
import datax.exception.EngineException
import datax.securedsetting.KeyVaultClient
import datax.utility.ConverterUtil._
import datax.utility.SinkerUtil
import org.apache.log4j.LogManager
import org.apache.spark.sql.SparkSession

class CosmosDBSinker(key: String, conf: CosmosDBConf) {
  private val logger = LogManager.getLogger(s"CosmosDBSinker-${key}")
  private val client = CosmosDBSinkerManager.getClient(conf.connectionString)
  logger.warn(s"Initialized")

  private def getDatabase(databaseName: String) = {
    val databases = client.queryDatabases(s"SELECT * FROM root r WHERE r.id='${databaseName}'", null)
      .getQueryIterable().toList
    if(databases.size()>0){
      databases.get(0)
    }
    else{
      try{
        val definition = new Database()
        definition.setId(databaseName)
        client.createDatabase(definition, null).getResource()
      }
      catch {
        case e: DocumentClientException => throw e
      }
    }
  }

  private val databaseLink = getDatabase(conf.database).getSelfLink

  private def getCollection(collectionName: String) = {
    val collections = client.queryCollections(databaseLink, s"SELECT * FROM root r WHERE r.id='$collectionName'", null)
      .getQueryIterable().toList
    if(collections.size()>0){
      collections.get(0)
    }
    else{
      try{
        val definition = new DocumentCollection()
        definition.setId(collectionName)
        client.createCollection(databaseLink, definition, null).getResource()
      }
      catch {
        case e: DocumentClientException => throw e
      }
    }
  }

  private val collectionLink = getCollection(conf.collection).getSelfLink

  def createDocument(json: String) = {
    val doc = new Document(json)

    try{
      client.createDocument(collectionLink, doc, null, false)
      true
    }
    catch {
      case e:DocumentClientException =>
        throw e
    }
  }

  private def createDocuments(jsons: Seq[String]) = {
    jsons.foreach(createDocument(_))
  }
}

object CosmosDBSinkerManager extends SinkOperatorFactory {
  private  val SinkName = "CosmosDB"
  private val logger = LogManager.getLogger("CosmosDBSinkerManager")
  private val pool = new ConcurrentHashMap[String, CosmosDBSinker]
  private val clientPool = new ConcurrentHashMap[String, DocumentClient]

  private val connectionStringRegex = "^AccountEndpoint=([^;]*);AccountKey=([^;]*);".r
  def parseConnectionString(conn: String) = {
    connectionStringRegex.findFirstMatchIn(conn).map(m=>(m.group(1), m.group(2)))
  }

  def getSinker(conf: CosmosDBConf) = {
    val key = conf.name
    pool.computeIfAbsent(key, (k: String) => new CosmosDBSinker(k, conf))
  }

  def getClient(connectionString: String) = {
    parseConnectionString(connectionString) match {
      case Some((serviceEndpoint, masterKey)) =>
        clientPool.computeIfAbsent(serviceEndpoint, (k: String) => {
          logger.warn(s"Create new client for serviceEndpoint:$k")
          new DocumentClient(serviceEndpoint, masterKey, null, null)
        })
      case None =>
        throw new EngineException(s"unexpected connection string:'${connectionString}'")
    }
  }

  def getClient(serviceEndpoint: String, masterKey: String) = {
    clientPool.computeIfAbsent(serviceEndpoint, (k: String) => {
      logger.warn(s"Create new client for serviceEndpoint:$k")
      new DocumentClient(serviceEndpoint, masterKey, null, null)
    })
  }


  def getSinkOperator(dict: SettingDictionary, name: String) : SinkOperator = {
    val conf = CosmosDBOutputSetting.buildCosmosDBOutputConf(dict, name)
    SinkOperator(
      name = SinkName,
      isEnabled = conf!=null,
      sinkAsJson = true,
      flagColumnExprGenerator = () => null,
      generator = flagColumnIndex => SinkerUtil.outputGenerator(
        (dataToSend:Seq[String],ls: String) => {
          val cosmosDBSinker = CosmosDBSinkerManager.getSinker(conf)
          dataToSend.count(d=>cosmosDBSinker.createDocument(d))
        },
        SinkName
      )(flagColumnIndex),
      onInitialization = (spark: SparkSession) => {
        val logger = LogManager.getLogger(this.getClass)
        CosmosDBSinkerManager.getSinker(conf)
        logger.warn(s"initialize cosmos DB sinker destination at ${conf.name}")
      }
    )
  }

  override def getSettingNamespace(): String = CosmosDBOutputSetting.Namespace
}
