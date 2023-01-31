// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.host

import org.apache.hadoop.fs.azurebfs.AzureBlobFileSystemStore
import org.apache.hadoop.fs.azurebfs.services.{AbfsClient, AbfsRestOperation}
import org.apache.log4j.{Level, LogManager}

object Logger {
  var logLevel = LogManager.getRootLogger.getLevel
  def setLogLevel(level: Level) = {
    logLevel = level
    val logger = LogManager.getRootLogger
    logger.setLevel(level)
    logger.warn(s"root logger level set to ${level}")
    LogManager.getLogger(classOf[AzureBlobFileSystemStore]).setLevel(level)
    LogManager.getLogger(classOf[AbfsClient]).setLevel(level)
    LogManager.getLogger(classOf[AbfsRestOperation]).setLevel(level)
  }
}
