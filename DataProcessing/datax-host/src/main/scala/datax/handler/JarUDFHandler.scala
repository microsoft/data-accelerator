// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.handler

import datax.config.{SettingDictionary, SettingNamespace}
import datax.host.SparkJarLoader
import datax.securedsetting.KeyVaultClient
import org.apache.log4j.LogManager
import org.apache.spark.sql.SparkSession


object JarUDFHandler {
  case class JarUDFConf(name: String, path: String, `class`: String, libs: List[String])

  val logger = LogManager.getLogger(this.getClass)
  val SettingJarUDF = "jar.udf"
  val SettingJarUDAF = "jar.udaf"
  val SettingJarUDFPath = "path"
  val SettingJarUDFClass = "class"
  val SettingJarUDFLibs = "libs"

  private def buildJarUdfConf(dict: SettingDictionary, name: String): JarUDFConf = {
    JarUDFConf(
      name = name,
      path = dict.getOrNull(SettingJarUDFPath),
      `class` = dict.getOrNull(SettingJarUDFClass),
      libs = dict.getStringSeqOption(SettingJarUDFLibs).map(_.toList).orNull
    )
  }

  private def buildJarUdfConfArray(dict: SettingDictionary, prefix: String): List[JarUDFConf] = {
    logger.warn("#### JarUDFHandler prefix=" +  prefix)
    dict.groupBySubNamespace(prefix)
      .map{ case(k, v) =>
        buildJarUdfConf(v, k)
      }
      .toList
  }

  def loadJarUdf(spark: SparkSession, dict: SettingDictionary) = {
    val jarUDFs = buildJarUdfConfArray(dict, SettingNamespace.JobProcessPrefix+SettingJarUDF+SettingNamespace.Seperator)
    val jarUDAFs = buildJarUdfConfArray(dict, SettingNamespace.JobProcessPrefix+SettingJarUDAF+SettingNamespace.Seperator)

    val libs = jarUDFs.flatMap(c=>if(c.libs==null) Seq(c.path) else c.libs:+c.path).toSet ++ jarUDAFs.flatMap(c=>if(c.libs==null) Seq(c.path) else c.libs:+c.path).toSet
    libs.foreach(libPath => {
      logger.warn(s"Adding JAR at $libPath to driver and executors")
      val actualPath = KeyVaultClient.resolveSecretIfAny(libPath)
      SparkJarLoader.addJar(spark, actualPath)
    })

    jarUDFs.foreach(udf => {
      logger.warn(s"###########jarUDFs class name ="+ udf.`class`)
      SparkJarLoader.registerJavaUDF(spark.udf, udf.name, udf.`class`, null)
    })

    jarUDAFs.foreach(udf=>{
      logger.warn(s"###########jarUDAFs class name ="+ udf.`class`)
      SparkJarLoader.registerJavaUDAF(spark.udf, udf.name, udf.`class`)
    })
  }
}
