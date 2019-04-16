// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.config

import org.apache.spark.{SparkEnv, TaskContext}

object SparkEnvVariables {
  def getClientNodeName(appName: String): String = {
    appName+"-"+SparkEnv.get.executorId
  }

  def getLoggerSuffix(): String = {
    val tc = TaskContext.get()
    if(tc==null)"" else s"-P${tc.partitionId()}-T${tc.taskAttemptId()}"
  }
}
