// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.handler

import datax.classloader.ClassLoaderHost
import datax.config.{SettingDictionary, SettingNamespace}
import datax.extension.StringNormalizer
import org.apache.log4j.LogManager
import org.apache.spark.sql.SparkSession

object InputNormalizerHandler {
  val logger = LogManager.getLogger(this.getClass)
  val SettingPreProjection = SettingNamespace.JobProcessPrefix + "inputnormalizer"
  def initialize(spark: SparkSession, dict: SettingDictionary) = {
    loadProcessor(spark,
        dict,
        "default",
        dict.get(SettingPreProjection).orNull)
  }

  def loadProcessor(spark:SparkSession, dict: SettingDictionary, processorName: String, className: String) = {
    if(className==null||className.isEmpty){
      logger.warn(s"no input normalizer processor is defined")
      null
    }
    else {
      logger.warn(s"loading class ${className} for input normalizer handler '${processorName}'")
      val clazz = ClassLoaderHost.classForName(className)
      val processor = clazz.newInstance().asInstanceOf[StringNormalizer]
      logger.warn(s"loaded class ${className} for input normalizer handler '${processorName}'")

      // transform the method to a delegate
      processor.normalize _
    }
  }
}