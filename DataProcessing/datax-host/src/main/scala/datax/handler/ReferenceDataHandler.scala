// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.handler

import datax.config.{SettingDictionary, SettingNamespace}
import datax.securedsetting.KeyVaultClient
import datax.telemetry.AppInsightLogger
import datax.utility.CSVUtil
import org.apache.log4j.LogManager
import org.apache.spark.sql.SparkSession

import scala.concurrent.{ExecutionContext, Future}

object ReferenceDataHandler {
  case class ReferenceDataConf(format:String, name:String, path:String, delimiter: Option[String], header: Option[String])
  val logger = LogManager.getLogger(this.getClass)
  val Namespace = "referencedata"
  val SettingFormat = "format"
  val SettingPath = "path"
  val SettingDelimiter = "delimiter"
  val SettingHeader = "header"

  private def buildReferenceDataConf(dict: SettingDictionary, name: String): ReferenceDataConf = {
    ReferenceDataConf(
      format = dict.getOrNull(SettingFormat),
      name = name,
      path = dict.getOrNull(SettingPath),
      delimiter = dict.get(SettingDelimiter),
      header = dict.get(SettingHeader)
    )
  }

  private def buildReferenceDataConfArray(dict: SettingDictionary, prefix: String): List[ReferenceDataConf] = {
    logger.warn("#### ReferenceDataHandler prefix:" +prefix)
    dict.groupBySubNamespace(prefix)
      .map{ case(k, v) => buildReferenceDataConf(v, k)}
      .toList
  }

  def loadReferenceDataFuture(spark: SparkSession, dict: SettingDictionary)(implicit ec: ExecutionContext): Future[Int] = {
    Future {
      val rds = buildReferenceDataConfArray(dict, SettingNamespace.JobInputPrefix+Namespace + SettingNamespace.Seperator)
      rds.foreach(rd => {
          val actualPath = KeyVaultClient.resolveSecretIfAny(rd.path)
          rd.format.toLowerCase() match {
            case "csv" => {
              CSVUtil.loadCSVReferenceData(spark, rd.format, actualPath, rd.name, rd.delimiter, rd.header, AppInsightLogger)
              true
            }
            case other: String => throw new Error(s"The ReferenceDataType:'$other' at path '${rd.path}' as specified in the configuration is not currently supported.")
          }
        })

      val count = rds.length
      logger.warn(s"Loaded ${count} reference data as tables")
      count
    }
  }
}
