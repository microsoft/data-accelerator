// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

package datax.sink

import datax.client.sql.SqlConf
import datax.config.SettingDictionary
import datax.utility.SinkerUtil
import org.apache.spark.sql.{DataFrame, Row}
import com.microsoft.azure.sqldb.spark.config.Config
import com.microsoft.azure.sqldb.spark.connect._

object SqlSinker extends SinkOperatorFactory  {

  val SinkName = "Sql"

  private def writeToSql(dataToSend:DataFrame, sqlConf: SqlConf, logSuffix:String) = {

    if(sqlConf.useBulkCopy) {
      writeUsingSqlBulkCopy(dataToSend,sqlConf,logSuffix)
      }
    else {
      writeUsingSqlConnector(dataToSend,sqlConf,logSuffix)
    }
  }

  private def writeUsingSqlJdbc(dataToSend:DataFrame, sqlConf: SqlConf, logSuffix:String) ={

    val sqlurl = sqlConf.connectionString
    val prop = new java.util.Properties
    prop.setProperty("driver", "com.microsoft.sqlserver.jdbc.SQLServerDriver")

    dataToSend.write.mode(sqlConf.writeMode).jdbc(sqlurl, sqlConf.table, prop)
    dataToSend.count().toInt
  }

  private def writeUsingSqlConnector(dataToSend:DataFrame, sqlConf: SqlConf, logSuffix:String) ={

    val cfgMap = getConfigMap(sqlConf)
    dataToSend.write.mode(sqlConf.writeMode).sqlDB(Config(cfgMap))
    dataToSend.count().toInt
  }


  private def writeUsingSqlBulkCopy(dataToSend:DataFrame, sqlConf: SqlConf, logSuffix:String) ={

    val bulkCopyConfig = getConfigMap(sqlConf)
    dataToSend.bulkCopyToSqlDB(Config(bulkCopyConfig))
    dataToSend.count().toInt
  }


  private def getConfigMap(sqlConf: SqlConf): Map[String,String] = {

    var configMap = Map(
      "url"                     -> sqlConf.url,
      "databaseName"            -> sqlConf.databaseName,
      "user"                    -> sqlConf.userName,
      "password"                -> sqlConf.password,
      "dbTable"                 -> sqlConf.table,
      "connectTimeout"          -> sqlConf.connectionTimeout,
      "queryTimeout"            -> sqlConf.queryTimeout
    )

    // encrypt and cert related settings goes hand in hand, all are needed to be set together
    if(sqlConf.encrypt!=null && sqlConf.trustServerCertificate!=null && sqlConf.hostNameInCertificate!=null)
    {
      configMap += (
        "encrypt"                 -> sqlConf.encrypt,
        "trustServerCertificate"  -> sqlConf.trustServerCertificate,
        "hostNameInCertificate"   -> sqlConf.hostNameInCertificate)
    }

    // if bulkCopy is enabled, add the corresponding settings
    if(sqlConf.useBulkCopy) {

      configMap += (
        "bulkCopyBatchSize"              -> sqlConf.bulkCopyBatchSize,
        "bulkCopyTableLock"              -> sqlConf.useBulkCopyTableLock,
        "bulkCopyTimeout"                -> sqlConf.bulkCopyTimeout,
        "bulkCopyUseInternalTransaction" -> sqlConf.useBulkCopyInternalTransaction)
    }

    configMap
  }

  def getSinkOperator(dict: SettingDictionary, name: String) : SinkOperator = {

    val conf = SqlOutputSetting.buildSqlOutputConf(dict, name)
    SinkOperator(
      name = SinkName,
      isEnabled = conf!=null,
      sinkAsJson = false,
      flagColumnExprGenerator = () => conf.filter,
      generator = (flagColumnIndex)=> getRowsSinkerGeneratorDf(conf)
    )
  }

  def getRowsSinkerGeneratorDf(sqlConf: SqlConf) : SinkDelegate = {
    val sender = (dataToSend:DataFrame, loggerSuffix:String) => writeToSql(dataToSend, sqlConf, SinkName)
    SinkerUtil.outputGenerator(sender,SinkName)()
  }

  override def getSettingNamespace(): String = SqlOutputSetting.Namespace
}
