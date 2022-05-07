// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.handler

import datax.config._
import datax.exception.EngineException
import datax.fs.HadoopClient
import datax.securedsetting.KeyVaultClient
import datax.utility.AzureFunctionCaller
import org.apache.log4j.LogManager
import org.apache.spark.sql.SparkSession

import scala.collection.mutable.HashMap

object StateTableHandler {
  case class StateTableConf(name: String, schema: String, location: String)

  val logger = LogManager.getLogger(this.getClass)
  val SettingStateTable = "statetable"
  val SettingStateTableSchema = "schema"
  val SettingStateTableLocation = "location"

  // table initialization
  val TableMetadata_Active = "active"
  val TableMetadata_Standby = "standby"
  private def readTableMetadata(metadataFile: String): HashMap[String, String] = {
    if(HadoopClient.fileExists(metadataFile)) {
      HashMap(
        HadoopClient.readHdfsFile(metadataFile).map(s => {
          val pos = s.indexOf('=')
          if (pos <= 0) {
            throw new EngineException(s"Invalid content in '${metadataFile}': '${s}'")
          }
          else {
            (s.substring(0, pos), s.substring(pos + 1, s.length))
          }
        }).toSeq: _*)
    }
    else{
      HadoopClient.ensureParentFolderExists(metadataFile)
      HashMap[String, String](
        TableMetadata_Active -> "A",
        TableMetadata_Standby -> "B"
      )
    }
  }

  private def writeTableMetadata(metadataFile: String, parameters: HashMap[String, String]): Unit ={
    HadoopClient.writeHdfsFile(metadataFile, parameters.map(i=>i._1+"="+i._2).mkString("\n"), overwriteIfExists=true, directWrite=false)
  }

  private def getTableNameVersioned(name: String, suffix: String) = name+"_"+suffix

  private def buildAccumulationTableConf(dict: SettingDictionary, name: String): StateTableConf = {
    StateTableConf(
      name = name,
      schema = dict.getOrNull(SettingStateTableSchema),
      location = dict.getOrNull(SettingStateTableLocation)
    )
  }

  private def buildAccumulationTableArrayConf(dict: SettingDictionary, prefix: String) ={
    dict.groupBySubNamespace(prefix)
      .map{ case(k, v) => buildAccumulationTableConf(v, k)}
      .toList
  }

  def createTables(spark:SparkSession, dict: SettingDictionary) = {
    buildAccumulationTableArrayConf(dict, SettingNamespace.JobProcessPrefix+SettingStateTable+SettingNamespace.Seperator)
      .map(t=>{
      val location = t.location.stripSuffix("/") + "/"
      val metadataFile = location + "metadata.info"
      val parameters = readTableMetadata(metadataFile)
      var parametersModified = false

      val tables = Seq("active", "standby").map(version => {
        val suffix = parameters.get(version).get
        val spec = s"STORED AS PARQUET LOCATION '${location+suffix}/'"
        val tableName = getTableNameVersioned(t.name, suffix)
        val sql =s"CREATE TABLE IF NOT EXISTS $tableName (${t.schema}) $spec"
        logger.warn(s"Creating '$version' Table ${t.name}: $sql")
        spark.sql(sql)

        suffix -> tableName
      }).toMap

      t.name -> new {
        private val tableLogger = LogManager.getLogger(s"TableStore-${t.name}")
        def getActiveTableName(): String = {
          getTableNameVersioned(t.name, parameters(TableMetadata_Active))
        }

        def getStandbyTableName(): String = {
          getTableNameVersioned(t.name, parameters(TableMetadata_Standby))
        }

        def overwrite(selectSql: String) = {
          val standbyTableName = getStandbyTableName()
          tableLogger.warn(s"Overwriting standby table: $standbyTableName")
          val sql = s"INSERT OVERWRITE TABLE $standbyTableName $selectSql"
          spark.sql(sql)
        }

        def flip():String = {
          parameters ++= Map(
            TableMetadata_Active -> parameters(TableMetadata_Standby),
            TableMetadata_Standby -> parameters(TableMetadata_Active)
          )
          parametersModified = !parametersModified
          val result = getActiveTableName()
          tableLogger.warn(s"Fliped active and standby, now active instance is ${result}")
          result
        }

        def persist() = {
          if(parametersModified) {
            tableLogger.warn(s"persisting parameters: ${parameters}")
            writeTableMetadata(metadataFile, parameters)
          }
          else{
            tableLogger.warn(s"Skip persisting parameters: ${parameters}")
          }
        }
      }
    }).toMap
  }
}
