// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.executor

import datax.config.ConfigManager
import datax.constants.{BlobProperties, JobArgument}
import datax.fs.HadoopClient
import datax.host.SparkJarLoader
import datax.securedsetting.KeyVaultClient
import org.apache.log4j.LogManager
import org.apache.spark.broadcast
import org.apache.spark.sql.SparkSession

object ExecutorHelper {
  private val logger = LogManager.getLogger(this.getClass)

  /***
    * Create broadcast variable for blob storage account key
    * @param path blob storage path
    * @param spark SparkSession
    */
  def createBlobStorageKeyBroadcastVariable(path: String, spark : SparkSession): broadcast.Broadcast[String] ={
    addJarToExecutor(spark)
    val sc = spark.sparkContext
    val sa = getStorageAccountName(path)
    var key = ""
    KeyVaultClient.withKeyVault {vaultName => key = HadoopClient.resolveStorageAccount(vaultName, sa).get}
    val blobStorageKey = sc.broadcast(key)
    blobStorageKey
  }

  /***
    * Get the storage account name from blob path
    * @param path blob storage path
    */
  private def getStorageAccountName(path:String):String ={
    val regex = s"@([a-zA-Z0-9-_]+)${BlobProperties.BlobHostPath}".r
    regex.findFirstMatchIn(path) match {
      case Some(partition) => partition.group(1)
      case None =>  null
    }
  }

  /***
    * Add azure-storage jar to executor nodes
    * @param spark Spark Session
    */
  private def addJarToExecutor(spark : SparkSession){
    try{
      logger.warn("Adding azure-storage jar to executor nodes")
      withStorageAccount {(storageAccount,containerName,azureStorageJarPath) => SparkJarLoader.addJar(spark, s"wasbs://$containerName@$storageAccount${BlobProperties.BlobHostPath}$azureStorageJarPath")}
    }
    catch {
      case e: Exception => {
        logger.error(s"azure-storage jar could not be added to executer nodes", e)
        throw e
      }
    }
  }

  /***
    * a scope to execute operation with the default storageAccount/container/azureStorageJarPath, skip the operation if that doesn't exist.
    * @param callback execution within the scope
    */
  private def withStorageAccount(callback: (String, String, String)=> Unit) = {
    ConfigManager.getActiveDictionary().get(JobArgument.ConfName_DefaultStorageAccount) match {
      case Some(storageAccount) =>
        logger.warn(s"Default Storage Account is $storageAccount")
        ConfigManager.getActiveDictionary().get(JobArgument.ConfName_DefaultContainer) match {
          case Some(containerName) =>
            logger.warn(s"Default container is $containerName")
            ConfigManager.getActiveDictionary().get(JobArgument.ConfName_AzureStorageJarPath) match {
              case Some(azureStorageJarPath) =>
                logger.warn(s"Azure storage jar path is $azureStorageJarPath")
                callback(storageAccount, containerName, azureStorageJarPath)
              case None => logger.warn(s"No azure storage jar path is defined")
            }
          case None => logger.warn(s"No default container is defined")
        }
      case None => logger.warn(s"No default storage account is defined")
    }
  }
}
