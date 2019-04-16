// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.host

import org.apache.log4j.{Level, LogManager}

object Logger {
  var logLevel = LogManager.getRootLogger.getLevel
  def setLogLevel(level: Level) = {
    logLevel = level
    val logger = LogManager.getRootLogger
    logger.setLevel(level)
    logger.warn(s"root logger level set to ${level}")
  }
}
