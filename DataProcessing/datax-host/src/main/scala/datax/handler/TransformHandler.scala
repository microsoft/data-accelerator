// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.handler

import datax.classloader.ClassLoaderUtils.{getClasspathFileLines, isClasspathFileUri}
import datax.config.{SettingDictionary, SettingNamespace}
import datax.fs.HadoopClient
import datax.securedsetting.KeyVaultClient
import datax.sql.{ParsedResult, TransformSQLParser}
import datax.utility.Validation
import org.apache.log4j.LogManager

import scala.concurrent.{ExecutionContext, Future}

object TransformHandler {
  private def getTransformFilePath(dict: SettingDictionary): Option[String] = {
    dict.get(SettingNamespace.JobProcessPrefix + "transform")
  }

  def shouldCacheCommonViews(dict: SettingDictionary): Boolean = {
    dict.getOrElse(SettingNamespace.JobProcessPrefix + "cachecommonviews", "True").toBoolean
  }

  def loadTransformFuture(dict: SettingDictionary)(implicit ec: ExecutionContext): Future[ParsedResult] = {
    val logger = LogManager.getLogger(this.getClass)
    getTransformFilePath(dict) match {
      case Some(transform) =>Future {
        logger.warn(s"Load transform script from '${transform}'")
        val filePath = KeyVaultClient.resolveSecretIfAny(transform)
        val sqlParsed = if(isClasspathFileUri(filePath)) {
          TransformSQLParser.parse(getClasspathFileLines(filePath).toSeq)
        }
        else {
          TransformSQLParser.parse(HadoopClient.readHdfsFile(filePath).toSeq)
        }

        if(sqlParsed!=null) {
          val queries = sqlParsed.commands.filter(_.commandType==TransformSQLParser.CommandType_Query)
          for (i <- 0 until queries.length) {
            val transformation = queries(i).text
            Validation.ensureNotNull(transformation, s"transform-$i")
            logger.warn(s"Transform step #$i = \n" + transformation)
          }

          sqlParsed.viewReferenceCount.foreach(v=>{
            logger.warn(s"View ${v._1} is referenced ${v._2} times")
          })
        }

        sqlParsed
      }
      case None => Future {
        logger.warn(s"Transform file is not defined.")
        null
      }
    }
  }
}
