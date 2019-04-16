// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.client.redis

import java.net.SocketAddress
import java.time.Duration
import java.util.concurrent.ConcurrentHashMap

import io.lettuce.core._
import io.lettuce.core.api.sync.{RedisSortedSetCommands, RedisStringCommands}
import io.lettuce.core.cluster.ClusterTopologyRefreshOptions.RefreshTrigger
import io.lettuce.core.cluster.{ClusterClientOptions, ClusterTopologyRefreshOptions, RedisClusterClient}
import datax.constants.ProductConstant
import datax.exception.EngineException
import org.apache.log4j.LogManager
import datax.utility.ConverterUtil.scalaFunctionToJava

case class RedisServerConf(name:String, host: String, port: Int, key: String, timeout: Int, useSsl: Boolean, isCluster: Boolean)

object RedisBase {
  val logger = LogManager.getLogger(this.getClass)

  /**
    * convert the redis cache connection string to type of @RedisServerConf
    * the format of the connection string should be like
    * <host>:<port>,password=<password>,ssl=True|False,cluster=True|False,timeout=<timeout>
    * @param connString connection string to parse
    * @return a RedisServerConf object
    */
  def parseConnectionString(connString: String): RedisServerConf = {
    if(connString == null || connString.isEmpty) return null
    val parts = connString.split(",")
    val hostAndPort = parts(0).trim.split(":")
    if(hostAndPort.length!=2) throw new EngineException(s"Malformed format of host and port in redis connection string")
    val host = hostAndPort(0)
    val port = hostAndPort(1).toInt

    val dict = parts.drop(1).map(p=>{
      val pos = p.indexOf("=")
      if(pos>0){
        p.substring(0, pos) -> p.substring(pos+1)
      }
      else
        throw new EngineException(s"Malformed format of parts in redis connection string")
    }).toMap

    RedisServerConf(
      name = host,
      host = host,
      port = port,
      key = dict.get("password").orNull,
      timeout = dict.getOrElse("timeout","3000").toInt,
      useSsl = dict.getOrElse("ssl", "True").toBoolean,
      isCluster = dict.getOrElse("cluster", "True").toBoolean
    )
  }

  def buildUri(conf: RedisServerConf, clientName: String = ProductConstant.ProductRedisBase) ={
    RedisURI.Builder.redis(conf.host)
      .withPassword(conf.key)
      .withPort(conf.port.toInt)
      .withSsl(conf.useSsl)
      .withVerifyPeer(false)
      .withClientName(clientName)
      .withTimeout(Duration.ofMillis(conf.timeout))
      .build()
  }

  def getClusterConnection(client: RedisClusterClient) = {
    val topologyRefreshOptions = ClusterTopologyRefreshOptions.builder()
        .enablePeriodicRefresh(false)
        //.refreshPeriod(Duration.ofMinutes(30))
        .enableAdaptiveRefreshTrigger(RefreshTrigger.MOVED_REDIRECT, RefreshTrigger.PERSISTENT_RECONNECTS)
        .adaptiveRefreshTriggersTimeout(Duration.ofSeconds(120))
        .build()

    val so = SocketOptions.builder()
      .connectTimeout(Duration.ofSeconds(120))
      .keepAlive(true)
      .tcpNoDelay(true)
      .build()

    client.setOptions(ClusterClientOptions.builder()
          .socketOptions(so)
          .validateClusterNodeMembership(false)
          .topologyRefreshOptions(topologyRefreshOptions)
          .build())

    client.connect
  }

  def getStandardConnection(client: RedisClient) = {
    client.connect()
  }

  private def getInternalConnection(conf: RedisServerConf, clientName: String):RedisConnection = {
    if (conf.isCluster)
      new RedisClusterConnection(conf, clientName)
    else
      new RedisStandardConnection(conf, clientName)
  }

  private val connectionPool = new ConcurrentHashMap[String, RedisConnection]
  def getConnection(connectionString: String, clientName: String) = {
    val conf = parseConnectionString(connectionString)
    connectionPool.computeIfAbsent(clientName, (k: String) => getInternalConnection(conf, clientName))
  }
}

class RedisStandardConnection(redisServerConf: RedisServerConf, clientName: String = ProductConstant.ProductRedisStandardConnection) extends RedisConnection{
  private val loggerPrefix = s"RedisClusterConnection-${redisServerConf.host}"
  private val logger = LogManager.getLogger(loggerPrefix)
  private val uri = RedisBase.buildUri(redisServerConf, clientName)
  private val client = RedisClient.create(uri)
  client.setDefaultTimeout(Duration.ofSeconds(120))

  logger.warn(s"${loggerPrefix}:Init connection to ${uri.getHost}")
  var connection = RedisBase.getStandardConnection(client)
  logger.info(s"${loggerPrefix}:Created connection")

  override def getStringCommands: RedisStringCommands[String, String] = {
    connection.sync()
  }

  override def getSortedSetCommands: RedisSortedSetCommands[String, String] = {
    connection.sync()
  }

  override def reconnect: Unit = {
    this.synchronized{
      logger.warn(s"${loggerPrefix}:Closing the connection for reconnect")
      connection.close()
      connection=RedisBase.getStandardConnection(client)
    }
  }
}

class RedisClusterConnection(redisServerConf: RedisServerConf, clientName: String = ProductConstant.ProductRedisClusterConnection) extends RedisConnection{
  private val loggerPrefix = s"RedisClusterConnection-${redisServerConf.host}"
  private val logger = LogManager.getLogger(loggerPrefix)
  private val uri = RedisBase.buildUri(redisServerConf, clientName)
  private val client = RedisClusterClient.create(uri)
  client.setDefaultTimeout(Duration.ofSeconds(120))

  logger.warn(s"${loggerPrefix}:Init connection")
  private var connection = RedisBase.getClusterConnection(client)

  client.addListener(new RedisConnectionStateListener(){
    override def onRedisConnected(connection: RedisChannelHandler[_, _], socketAddress: SocketAddress): Unit = {
      logger.warn(s"${loggerPrefix}:Connected with socket:${socketAddress}")
    }

    override def onRedisDisconnected(redisChannelHandler: RedisChannelHandler[_, _]): Unit = {
      logger.warn(s"${loggerPrefix}:Lost connection")
    }

    override def onRedisExceptionCaught(redisChannelHandler: RedisChannelHandler[_, _], throwable: Throwable): Unit = {
      logger.error(s"${loggerPrefix}:Encounter exception", throwable)
    }
  })

  logger.info(s"${loggerPrefix}:Created connection")
  override def getStringCommands: RedisStringCommands[String, String] = {
    connection.sync()
  }

  override def getSortedSetCommands: RedisSortedSetCommands[String, String] = {
    connection.sync()
  }

  override def reconnect: Unit = {
    this.synchronized{

      logger.warn(s"${loggerPrefix}:Closing the connection for reconnect")
      connection.close()
      connection=RedisBase.getClusterConnection(client)
    }
  }
}
