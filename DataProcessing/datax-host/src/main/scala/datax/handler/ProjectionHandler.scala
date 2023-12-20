// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.handler

import datax.config.ConfigManager.loadConfigFile
import datax.config.{SettingDictionary, SettingNamespace}
import datax.fs.HadoopClient
import datax.securedsetting.KeyVaultClient
import datax.utility.Validation
import org.apache.log4j.LogManager

import scala.concurrent.{ExecutionContext, Future}

object ProjectionHandler {
  val logger = LogManager.getLogger(this.getClass)
  val SettingProjection = "projection"
  
  private def getProjectionFilePaths(dict: SettingDictionary): Option[Seq[String]] = {
    dict.getStringSeqOption(SettingNamespace.JobProcessPrefix + SettingProjection)
  }

  def loadProjectionsFuture(dict: SettingDictionary)(implicit ec: ExecutionContext): Future[Seq[Seq[String]]] = {
    getProjectionFilePaths(dict) match {
      case Some(projections) => {
        Future.sequence(projections.map(projectionFile => Future {
          logger.warn(s"Load projection file from '$projectionFile'")
          val filePath = KeyVaultClient.resolveSecretIfAny(projectionFile)
          loadConfigFile(filePath).toSeq
        }))
      }
      case None => Future {
        Seq()
      }
    }
  }

  def validateProjections(projections: Seq[Seq[String]]) = {
    for(i <- 0 until projections.length){
      val expr = projections(i)
      Validation.ensureNotNull(expr, s"projection-$i")
      logger.warn(s"Projection Step #$i = \n" + expr.mkString("\n"))
    }
  }
}
