// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.fs

import java.io._
import java.net.URI
import java.nio.channels.FileChannel
import java.nio.file.Files
import java.util.concurrent.{Executors, TimeUnit}
import java.util.zip.GZIPInputStream
import com.google.common.io.{Files => GFiles}
import datax.config.SparkEnvVariables
import datax.constants.{BlobProperties, ProductConstant}
import datax.exception.EngineException
import datax.securedsetting.KeyVaultClient
import datax.telemetry.AppInsightLogger
import org.apache.commons.io.FileUtils
import org.apache.hadoop.conf.Configuration
import org.apache.hadoop.fs.{FileStatus, FileSystem, Path, RemoteIterator}
import org.apache.log4j.LogManager
import org.apache.spark.broadcast

import java.util.UUID
import scala.language.implicitConversions
import scala.collection.mutable
import scala.collection.mutable.ListBuffer
import scala.concurrent.duration.Duration
import scala.concurrent.{Await, ExecutionContext, Future, TimeoutException}
import scala.io.Source
import scala.util.Try

object HadoopClient {
  val logger = LogManager.getLogger(this.getClass)
  private val threadPool = Executors.newFixedThreadPool(5)
  implicit private val ec = ExecutionContext.fromExecutorService(threadPool)

  private var hadoopConf:Configuration = null

  /***
    * initialize the cached hadoop configuration
    * @param conf hadoop configuration for initialization
    */
  def setConf(conf: Configuration = null): Unit ={
    if(conf==null) {
      hadoopConf = new Configuration()
      //Used to fetch fileSystem for wasbs
      hadoopConf.set("fs.wasbs.impl","org.apache.hadoop.fs.azure.NativeAzureFileSystem")
      hadoopConf.set("fs.azure.io.retry.max.retries", "3")
    }
    else
      hadoopConf = conf
  }

  /***
    * get the cached hadoop configuration
    * @return the cached hadoop configuration
    */
  def getConf() = {
    if(hadoopConf==null)
      this.synchronized{
        if(hadoopConf==null)
          setConf()
      }

    hadoopConf
  }

  /***
    * get the name of storage account from a wasb-format or abfss-format path
    * @param path a hdfs path
    * @return the storage account name if there is storage account name in the wasbs/wasb or abfs/abfss path, else null
    */
  private def getStorageAccount(path: String): String = {
    val uri = Try(new URI(path.replace(" ", "%20"))).toOption
    if(uri.isDefined) {
      val scheme = uri.get.getScheme

      if (scheme == "wasb" || scheme == "wasbs" || scheme == "abfs" || scheme == "abfss")
        Option(uri.get.getHost) match {
          case Some(host) => host.toLowerCase().replaceAll(s"(${BlobProperties.BlobHostPath}|${BlobProperties.DfsHostPath})", "")
          case None => null
        }
      else
        null
    }
    else {
      null
    }
  }

  /***
    * get a distinct set of storage accounts from a list of file paths
    * @param paths a list of hdfs paths which might contains wasb/wasbs or abfs/abfss paths
    * @return a distinct set of names of storage accounts
    */
  private def getStorageAccounts(paths: Seq[String]): Set[String] = {
    paths.map(getStorageAccount _).filter(_!=null).toSet
  }

  /***
    * internal cache of storage keys against storage account names.
    */
  private val storageAccountKeys = new mutable.HashMap[String, String]

  /***
    * set key for storage account for azure-hadoop adapter to access that later
    * @param sa name of the storage account
    * @param key key to the storage account
    * @param isDfs true if dfs path, false for blob
    */
  private def setStorageAccountKey(sa: String, key: String, isDfs: Boolean = false): Unit ={
    storageAccountKeys.synchronized{
    storageAccountKeys += sa->key
    }

    // get the default storage account
    val defaultFS = getConf().get("fs.defaultFS","")
    // set the key only if its a non-default storage account
    if((isDfs && !defaultFS.toLowerCase().contains(s"$sa${BlobProperties.DfsHostPath}")) || (!isDfs && !defaultFS.toLowerCase().contains(s"$sa${BlobProperties.BlobHostPath}"))) {
      logger.warn(s"Setting the key in hdfs conf for storage account $sa")
      setStorageAccountKeyOnHadoopConf(sa, key, isDfs)
    }
    else {
      logger.warn(s"Default storage account $sa found, skipping setting the key")
    }
  }

  /***
    * resolve key for storage account with a keyvault name
    * warn if key is not found but we let it continue so static key settings outside of the job can still work
    * @param vaultName key vault name to get the key of storage account
    * @param sa name of the storage account
    * @param blobStorageKey broadcasted storage account key
    * @param isDfs true if dfs path, false for blob
    */
  def resolveStorageAccount(vaultName: String, sa: String, blobStorageKey: broadcast.Broadcast[String] = null, isDfs: Boolean = false) : Option[String] = {
    if(blobStorageKey != null) {
      setStorageAccountKey(sa, blobStorageKey.value, isDfs)
      Some(blobStorageKey.value)
    }
    else {
      // Fetch secret from keyvault using KeyVaultMsiAuthenticatorClient and if that does not return secret then fetch it using secret scope
      val secretId = s"keyvault://$vaultName/${ProductConstant.ProductRoot}-sa-$sa"
      KeyVaultClient.getSecret(secretId) match {
        case Some(value)=>
          logger.warn(s"Retrieved key for storage account '$sa' with secretid:'$secretId'")
          setStorageAccountKey(sa, value, isDfs)
          Some(value)
        case None =>
          val databricksSecretId = s"secretscope://$vaultName/${ProductConstant.ProductRoot}-sa-$sa"
          KeyVaultClient.getSecret(databricksSecretId) match {
            case Some(value)=>
              logger.warn(s"Retrieved key for storage account '$sa' with secretid:'$databricksSecretId'")
              setStorageAccountKey(sa, value, isDfs)
              Some(value)
            case None =>
              logger.warn(s"Failed to find key for storage account '$sa' with secretid:'$secretId' and '$databricksSecretId'")
              None
          }
      }
    }
  }

  /***
    * set key for storage account required by the specified hdfs path
    * @param path hdfs file to resolve the key of storage account if it is a valid wasb/wasbs path, do nothing if it isn't
    * @param blobStorageKey broadcasted storage account key
    */
  private def resolveStorageAccountKeyForPath(path: String, blobStorageKey: broadcast.Broadcast[String] = null) = {
    val sa = getStorageAccount(path)
    val uri = Try(new URI(path.replace(" ", "%20"))).toOption
    if(uri.isDefined) {
      val scheme = uri.get.getScheme

      if (sa != null && !sa.isEmpty) {
        if (scheme == "wasb" || scheme == "wasbs")
          KeyVaultClient.withKeyVault { vaultName => resolveStorageAccount(vaultName, sa, blobStorageKey, false) }
        else if (scheme == "abfs" || scheme == "abfss")
        // abfs protocol use Managed Identity for authentication
          None
      }
    }
    else {
      None
    }
  }

  /***
    * resolve key for storage accounts required by the specified hdfs paths
    * @param paths a list of hdfs paths, do nothing if there isn't any valid wasb/wasbs paths
    */
  private def resolveStorageAccountKeysForPaths(paths: Seq[String]) = {
    val storageAccounts = getStorageAccounts(paths)
      .filter(p=>p!=null & !p.isEmpty)
      .filterNot(storageAccountKeys.contains(_)) //TODO: make storageAccountKeys thread-safe

    if(!storageAccounts.isEmpty){
      KeyVaultClient.withKeyVault {vaultName => storageAccounts.foreach(sa=>resolveStorageAccount(vaultName, sa))}
    }
  }

  /***
    * export storage account keys to a immutable dictionary for serialization
    * @param paths hdfs paths to determine the storage accounts we need
    * @return storage accounts and corresponding keys resolved from the input hdfs paths
    */
  private def exportWasbKeys(paths: Seq[String]): Map[String, String] = {
    //TODO: make storageAccountKeys thread-safe
    getStorageAccounts(paths).map(sa => sa->storageAccountKeys.getOrElse(sa, null))
      .filter(_._2!=null)
      .toMap
  }

  /**
    * Return a Hadoop FileSystem with the scheme encoded in the given path.
    * @param path hdfs path to determine the file system from
    * @param conf hadoop configuration for the determination
    */
  private def getHadoopFileSystem(path: URI, conf: Configuration): FileSystem = {
    FileSystem.get(path, conf)
  }

  /***
    * read local file (non-hadoop) from disk if it exists
    * @param fileName path to the local file
    * @return content of the file if it exists, else null.
    */
  def readLocalFileIfExists(fileName: String): String = {
    val file = new File(fileName)
    if(file.exists()){
      val openFile = Source.fromFile(file)
      val result = openFile.getLines().mkString
      openFile.close()
      result
    }
    else{
      null
    }
  }

  private def readLocalFile(fileName: String): String = {
    val file = Source.fromFile(fileName)
    val result = file.getLines().mkString
    file.close()
    result
  }

  def fileExists(hdfsPath: String): Boolean = {
    val path = new Path(hdfsPath)
    val fs = path.getFileSystem(getConf())
    fs.exists(path)
  }

  /***
    * read a hdfs file
    * @param hdfsPath path to the hdfs file
    * @param gzip whether it is a gzipped file
    * @param blobStorageKey storage account key broadcast variable
    * @throws IOException if any
    * @return a iterable of strings from content of the file
    */
  @throws[IOException]
  def readHdfsFile(hdfsPath: String, gzip:Boolean=false, blobStorageKey: broadcast.Broadcast[String] = null): Iterable[String] = {
    val logger = LogManager.getLogger(s"FileLoader${SparkEnvVariables.getLoggerSuffix()}")

    // resolve key to access azure storage account
    resolveStorageAccountKeyForPath(hdfsPath, blobStorageKey)

    val lines = new ListBuffer[String]
    val t1= System.nanoTime()
    logger.info(s"Loading '$hdfsPath'")

    try{
      val path = new Path(hdfsPath)
      val fs = path.getFileSystem(getConf())
      val is = fs.open(path)
      // if empty blob found, then just return the lines
      if (is.available() == 0) {
        logger.warn(s"Found empty file: $path")
        return lines
      }

      //val source = Source.fromInputStream(is)
      val inputStream = if(gzip)new GZIPInputStream(is) else is
      val reader = new BufferedReader(new InputStreamReader(inputStream))

      try{
        //source.getLines().toList
        var line = reader.readLine()
        while(line!=null){
          lines += line
          line = reader.readLine()
        }
      }
      finally {
        reader.close()
      }
    }
    catch {
      case e: Exception =>{
        logger.error(s"Error in reading '$hdfsPath'", e)
        AppInsightLogger.trackException(e, Map(
          "errorLocation" -> "readHdfsFile",
          "errorMessage" -> "Error in reading file",
          "failedHdfsPath" -> hdfsPath
        ), null)

        throw e
      }
    }

    val elapsedTime = (System.nanoTime()-t1)/1E9
    logger.info(s"Done loading '$hdfsPath', count: ${lines.size}, elapsed time: $elapsedTime seconds")

    //TODO: return a iterator instead of the entire list to reduce memory consumption, may also possibly help optimize job performance
    lines
  }

  /**
    * write string content to a specified hdfs path
    * @param hdfsPath path to the specified hdfs file
    * @param content string content to write into the file
    * @param overwriteIfExists flag to specify if the file needs to be overwritten if it already exists in hdfs
    * @param directWrite flag to specify if the file needs to be created directly without renaming from a temp file
    * @throws IOException if any occurs in the write operation
    */
  @throws[IOException]
  def writeHdfsFile(hdfsPath: String, content: String, overwriteIfExists:Boolean, directWrite:Boolean) {
    writeHdfsFile(hdfsPath, content.getBytes("UTF-8"), getConf(), overwriteIfExists, directWrite)
  }

  /**
    * generate a random file name
    * @return a random file name of 8 characers.
    */
  private def randomFileName():String = {
    java.util.UUID.randomUUID().toString.substring(0, 8)
  }

  /**
    * generate a random string for prefixing a temp file name
    * @return a full random uuid
    */
  def tempFilePrefix: String = {
    //DigestUtils.sha256Hex(seed).substring(0, 8)
    UUID.randomUUID().toString
  }

  /**
    * write to a specified hdfs file with retries
    * @param hdfsPath the specified hdfs file
    * @param content conent to write into the file
    * @param timeout timeout duration for the write operation, by default 5 seconds
    * @param retries times in retries, by default 0 meaning no retries.
    * @param blobStorageKey storage account key broadcast variable
    */
  def writeWithTimeoutAndRetries(hdfsPath: String,
                                 content: Array[Byte],
                                 timeout: Duration = Duration(5, TimeUnit.SECONDS),
                                 retries: Int = 0,
                                 blobStorageKey: broadcast.Broadcast[String]
                                ) = {
    val logger = LogManager.getLogger(s"FileWriter${SparkEnvVariables.getLoggerSuffix()}")
    def f = Future{
      writeHdfsFile(hdfsPath, content, getConf(), overwriteIfExists=false, directWrite=false, blobStorageKey)
    }
    var remainingAttempts = retries+1
    while(remainingAttempts>0) {
      try {
        remainingAttempts -= 1
        logger.info(s"writing to $hdfsPath with remaining attempts: $remainingAttempts")
        Await.result(f, timeout)
        remainingAttempts = 0
      }
      catch {
        case e: TimeoutException =>
          remainingAttempts = 0
          AppInsightLogger.trackException(e, Map(
            "errorLocation" -> "writeWithTimeoutAndRetries",
            "errorMessage" -> "Error in writing file",
            "failedHdfsPath" -> hdfsPath
          ), null)
          throw e
        case e: IOException =>
          remainingAttempts = 0
          AppInsightLogger.trackException(e, Map(
            "errorLocation" -> "writeWithTimeoutAndRetries",
            "errorMessage" -> "Error in writing file",
            "failedHdfsPath" -> hdfsPath
          ), null)
          throw e
        case unknown: Exception =>
          remainingAttempts = 0
          AppInsightLogger.trackException(unknown, Map(
            "errorLocation" -> "writeWithTimeoutAndRetries",
            "errorMessage" -> "Unknown error in writing file",
            "failedHdfsPath" -> hdfsPath
          ), null)
          throw unknown
      }
    }
  }

  /**
    * set storage account key on hadoop conf
    * @param sa storage account name
    * @param value storage account key
    * @param isDfs true if dfs path, false for blob
    */
  private def setStorageAccountKeyOnHadoopConf(sa: String, value: String, isDfs: Boolean = false): Unit = {
    if(isDfs){
      getConf().set(s"fs.azure.account.key.$sa${BlobProperties.DfsHostPath}", value)
      getConf().set(s"fs.azure.account.keyprovider.$sa${BlobProperties.DfsHostPath}", "org.apache.hadoop.fs.azure.SimpleKeyProvider") // not decrypting access key
    }
    else{
      getConf().set(s"fs.azure.account.key.$sa${BlobProperties.BlobHostPath}", value)
      getConf().set(s"fs.azure.account.keyprovider.$sa${BlobProperties.BlobHostPath}", "org.apache.hadoop.fs.azure.SimpleKeyProvider") // not decrypting access key
    }
  }

  /**
    * make sure parent folder exists for path, create the folder if it doesn't exist
    * @param path specified path to check its parent folder
    */
  def ensureParentFolderExists(path: String): Unit = {
    val file = new Path(path)
    val folder = file.getParent
    val fs = folder.getFileSystem(getConf())
    if(!fs.exists(folder)){
      fs.mkdirs(folder)
    }
  }

  /**
   * Auxiliary method to build a temporary file path name
   * @param path The path to the file
   * @param prefix An optional prefix for the file path
   * @return A temporary file path
   */
  def getTemporaryPath(path: Path, prefix: String = ""): String = {
    s"$prefix/$tempFilePrefix-${path.getName}"
  }

  /**
   * Generates a temporary file path for the provided local file system file
   * @param path The path to the file
   * @return The temporary path for the provided file
   */
  def getLocalFileSystemTemporaryPath(path: Path): String = {
    s"file:///${getTemporaryPath(path, FileUtils.getTempDirectory.getPath)}"
  }

  /**
   * Generates a generic temporary file path for file system that does not require a special treatment
   * @param path The path to the file
   * @return The temporary path for the provided file
   */
  def getDefaultTemporaryPath(path: Path): String = {
    getTemporaryPath(path, "/_$tmpHdfsFolder$")
  }

  /**
   * Generate a temporary file path location for the provided file information
   * @param fs The file filesystem
   * @param uri The file uri
   * @param path The file path
   * @return A generated temporary folder location for the provided file
   */
  def createTempFilePathUri(fs: FileSystem, uri: URI, path: Path): URI = {
    val logger = LogManager.getLogger("createTempFilePathUri")
    if(fs.isInstanceOf[org.apache.hadoop.fs.LocalFileSystem]) {
      logger.info(s"Generating temporary local file path for ${fs.getClass}")
      // From local runs that uses those classes as FS implementation
      new URI(uri.getScheme, uri.getAuthority, getLocalFileSystemTemporaryPath(path), null, null)
    }
    else {
      logger.info(s"Generating temporary non-local file path for ${fs.getClass}")
      // Other FS including HDFS, use the normal method of generating the uri from scheme and authority
      new URI(uri.getScheme, uri.getAuthority, getDefaultTemporaryPath(path), null, null)
    }
  }

  /**
    * write content to a hdfs file
    * @param hdfsPath path to the specified hdfs file
    * @param content content to write into the file
    * @param conf hadoop configuration
    * @param overwriteIfExists flag to specify if the file needs to be overwritten if it already exists in hdfs
    * @param directWrite flag to specify if the file needs to be created directly without renaming from a temp file
    * @param blobStorageKey storage account key broadcast variable
    * @throws IOException if any from lower file system operation
    */
  @throws[IOException]
  private def writeHdfsFile(hdfsPath: String, content: Array[Byte], conf: Configuration, overwriteIfExists:Boolean, directWrite:Boolean, blobStorageKey: broadcast.Broadcast[String] = null) {
    resolveStorageAccountKeyForPath(hdfsPath, blobStorageKey)

    val logger = LogManager.getLogger("writeHdfsFile")

    val path = new Path(hdfsPath)
    val uri = path.toUri
    val fsy = path.getFileSystem(conf)

    // If output file already exists and overwrite flag is not set, bail out
    if(fsy.exists(path) && !overwriteIfExists){
      logger.warn(s"Output file ${path} already exists and overwrite flag ${overwriteIfExists}. Skipping writing again .")
      return
    }

    val fs = path.getFileSystem(conf)

    // If output file already exists and overwrite flag is set, delete old file and then rewrite new file
    if(fs.exists(path) && overwriteIfExists){
      logger.warn(s"Output file ${path} already exists and overwrite flag ${overwriteIfExists}. Deleting it.")
      fs.delete(path, true)
    }

    // If directWrite is true, write the file directly to the target path without renaming from a temp file
    if (directWrite) {
      val outStream = fs.create(path, true)
      if (content.length > 0) {
        val bs = new BufferedOutputStream(outStream)
        bs.write(content)
        bs.close()
      }
    } else {
      val tempHdfsPath = createTempFilePathUri(fs, uri, path)
      //val pos = hdfsPath.lastIndexOf('/')
      //val tempHdfsPath = hdfsPath.patch(pos, "/_temporary", 0)
      // TODO: create unique name for each temp file.
      val tempPath = new Path(tempHdfsPath)

      val bs = new BufferedOutputStream(fs.create(tempPath, true))
      bs.write(content)
      bs.close()

      if(!fs.rename(tempPath, path)) {
        // Rename failed, check if it was due to destination path already exists.
        // If yes, fail only if overwrite is set. If destination does not exist, then fail as-well.
        val fileExists = fs.exists(path)

        if (!fileExists || (fileExists && overwriteIfExists)) {
          val parent = path.getParent
          val msg = if(fs.exists(parent)) s"Move ${tempPath} to ${path} did not succeed"
          else s"Move ${tempPath} to ${path} did not succeed since parent folder does not exist!"
          throw new IOException(msg)
        }
        else {
          logger.warn(s"Blob rename from ${tempPath} to ${path} failed, but moving on since target already exists and overwrite is set to false.")
        }
      }
    }
  }

  /**
    * create a folder at the specified path
    * @param folderPath path to create the folder
    */
  def createFolder(folderPath: String): Unit ={
    resolveStorageAccountKeyForPath(folderPath)
    val path = new Path(folderPath)
    val fs = path.getFileSystem(getConf())
    fs.mkdirs(path)
  }

  /**
    * implict convert RemoteIterator to Iterator
    * @param underlying the underlying RemoteIterator instance
    * @tparam T type of the element in Iterator
    * @return a Iterator instance
    */
  implicit def convertToScalaIterator[T](underlying: RemoteIterator[T]): Iterator[T] = {
    case class wrapper(underlying: RemoteIterator[T]) extends Iterator[T] {
      override def hasNext = underlying.hasNext

      override def next = underlying.next
    }
    wrapper(underlying)
  }

  /**
    * list files under a folder
    * @param folder path to the specified folder
    * @return a list of file paths under the folder
    */
  def listFiles(folder: String): Iterator[String] = {
    listFileObjects(folder).map(f=>f.getPath.toString)
  }

  /**
   * list file objects under a folder
   *
   * @param folder path to the specified folder
   * @return a list of file paths under the folder
   */
  def listFileObjects(folder: String): Iterator[FileStatus] = {
    resolveStorageAccountKeyForPath(folder)
    val path = new Path(folder)
    val fs = path.getFileSystem(getConf)

    if (fs.exists(path))
      fs.listFiles(path, true)
    else
      Iterator.empty
  }

  /*
  * This function is copied from Apache Spark source code located at https://github.com/apache/spark/blob/master/core/src/main/scala/org/apache/spark/util/Utils.scala
  * Copy of the apache license can be obtained from http://www.apache.org/licenses/LICENSE-2.0
  * */
  /**
    * Execute a block of code, then a finally block, but if exceptions happen in
    * the finally block, do not suppress the original exception.
    *
    * This is primarily an issue with `finally { out.close() }` blocks, where
    * close needs to be called to clean up `out`, but if an exception happened
    * in `out.write`, it's likely `out` may be corrupted and `out.close` will
    * fail as well. This would then suppress the original/likely more meaningful
    * exception from the original `out.write` call.
    */
  def tryWithSafeFinally[T](block: => T)(finallyBlock: => Unit): T = {
    var originalThrowable: Throwable = null
    try {
      block
    } catch {
      case t: Throwable =>
        // Purposefully not using NonFatal, because even fatal exceptions
        // we don't want to have our finallyBlock suppress
        originalThrowable = t
        throw originalThrowable
    } finally {
      try {
        finallyBlock
      } catch {
        case t: Throwable if (originalThrowable != null && originalThrowable != t) =>
          originalThrowable.addSuppressed(t)
          val logger = LogManager.getLogger("TryWithSafe")
          logger.warn(s"Suppressing exception in finally: ${t.getMessage}", t)
          throw originalThrowable
      }
    }
  }


  /*
  * This function is copied from Apache Spark source code located at https://github.com/apache/spark/blob/master/core/src/main/scala/org/apache/spark/util/Utils.scala
  * Copy of the apache license can be obtained from http://www.apache.org/licenses/LICENSE-2.0
  * */
  def copyFileStreamNIO(
                         input: FileChannel,
                         output: FileChannel,
                         startPosition: Long,
                         bytesToCopy: Long): Unit = {
    val initialPos = output.position()
    var count = 0L
    // In case transferTo method transferred less data than we have required.
    while (count < bytesToCopy) {
      count += input.transferTo(count + startPosition, bytesToCopy - count, output)
    }
    assert(count == bytesToCopy,
      s"request to copy $bytesToCopy bytes, but actually copied $count bytes.")

    // Check the position after transferTo loop to see if it is in the right position and
    // give user information if not.
    // Position will not be increased to the expected length after calling transferTo in
    // kernel version 2.6.32, this issue can be seen in
    // https://bugs.openjdk.java.net/browse/JDK-7052359
    // This will lead to stream corruption issue when using sort-based shuffle (SPARK-3948).
    val finalPos = output.position()
    val expectedPos = initialPos + bytesToCopy
    assert(finalPos == expectedPos,
      s"""
         |Current position $finalPos do not equal to expected position $expectedPos
         |after transferTo, please check your kernel version to see if it is 2.6.32,
         |this is a kernel bug which will lead to unexpected behavior when using transferTo.
         |You can set spark.file.transferTo = false to disable this NIO feature.
           """.stripMargin)
  }

   /*
  * This function is copied from Apache Spark source code located at https://github.com/apache/spark/blob/master/core/src/main/scala/org/apache/spark/util/Utils.scala
  * Copy of the apache license can be obtained from http://www.apache.org/licenses/LICENSE-2.0
  * */
  /**
    * Copy all data from an InputStream to an OutputStream. NIO way of file stream to file stream
    * copying is disabled by default unless explicitly set transferToEnabled as true,
    * the parameter transferToEnabled should be configured by spark.file.transferTo = [true|false].
    */
  def copyStream(
                  in: InputStream,
                  out: OutputStream,
                  closeStreams: Boolean = false,
                  transferToEnabled: Boolean = false): Long = {
    tryWithSafeFinally {
      if (in.isInstanceOf[FileInputStream] && out.isInstanceOf[FileOutputStream]
        && transferToEnabled) {
        // When both streams are File stream, use transferTo to improve copy performance.
        val inChannel = in.asInstanceOf[FileInputStream].getChannel()
        val outChannel = out.asInstanceOf[FileOutputStream].getChannel()
        val size = inChannel.size()
        copyFileStreamNIO(inChannel, outChannel, 0, size)
        size
      } else {
        var count = 0L
        val buf = new Array[Byte](8192)
        var n = 0
        while (n != -1) {
          n = in.read(buf)
          if (n != -1) {
            out.write(buf, 0, n)
            count += n
          }
        }
        count
      }
    } {
      if (closeStreams) {
        try {
          in.close()
        } finally {
          out.close()
        }
      }
    }
  }

   /*
  * This function is copied from Apache Spark source code located at https://github.com/apache/spark/blob/master/core/src/main/scala/org/apache/spark/util/Utils.scala
  * Copy of the apache license can be obtained from http://www.apache.org/licenses/LICENSE-2.0
  * */
  /**
    * Copy `sourceFile` to `destFile`.
    *
    * If `destFile` already exists:
    *   - no-op if its contents equal those of `sourceFile`,
    *   - throw an exception if `fileOverwrite` is false,
    *   - attempt to overwrite it otherwise.
    *
    * @param url URL that `sourceFile` originated from, for logging purposes.
    * @param sourceFile File path to copy/move from.
    * @param destFile File path to copy/move to.
    * @param fileOverwrite Whether to delete/overwrite an existing `destFile` that does not match
    *                      `sourceFile`
    * @param removeSourceFile Whether to remove `sourceFile` after / as part of moving/copying it to
    *                         `destFile`.
    */
  private def copyFile(
                        url: String,
                        sourceFile: File,
                        destFile: File,
                        fileOverwrite: Boolean,
                        removeSourceFile: Boolean = false): Unit = {

    val logger = LogManager.getLogger("CopyFile")
    if (destFile.exists) {
      if (!filesEqualRecursive(sourceFile, destFile)) {
        if (fileOverwrite) {
          logger.info(
            s"File $destFile exists and does not match contents of $url, replacing it with $url"
          )
          if (!destFile.delete()) {
            throw new EngineException(
              "Failed to delete %s while attempting to overwrite it with %s".format(
                destFile.getAbsolutePath,
                sourceFile.getAbsolutePath
              )
            )
          }
        } else {
          throw new EngineException(
            s"File $destFile exists and does not match contents of $url")
        }
      } else {
        // Do nothing if the file contents are the same, i.e. this file has been copied
        // previously.
        logger.info(
          "%s has been previously copied to %s".format(
            sourceFile.getAbsolutePath,
            destFile.getAbsolutePath
          )
        )
        return
      }
    }

    // The file does not exist in the target directory. Copy or move it there.
    if (removeSourceFile) {
      Files.move(sourceFile.toPath, destFile.toPath)
    } else {
      logger.info(s"Copying ${sourceFile.getAbsolutePath} to ${destFile.getAbsolutePath}")
      copyRecursive(sourceFile, destFile)
    }
  }

  /*
* This function is copied from Apache Spark source code located at https://github.com/apache/spark/blob/master/core/src/main/scala/org/apache/spark/util/Utils.scala
* Copy of the apache license can be obtained from http://www.apache.org/licenses/LICENSE-2.0
* */
  private def filesEqualRecursive(file1: File, file2: File): Boolean = {
    if (file1.isDirectory && file2.isDirectory) {
      val subfiles1 = file1.listFiles()
      val subfiles2 = file2.listFiles()
      if (subfiles1.size != subfiles2.size) {
        return false
      }
      subfiles1.sortBy(_.getName).zip(subfiles2.sortBy(_.getName)).forall {
        case (f1, f2) => filesEqualRecursive(f1, f2)
      }
    } else if (file1.isFile && file2.isFile) {
      GFiles.equal(file1, file2)
    } else {
      false
    }
  }

 /*
* This function is copied from Apache Spark source code located at https://github.com/apache/spark/blob/master/core/src/main/scala/org/apache/spark/util/Utils.scala
* Copy of the apache license can be obtained from http://www.apache.org/licenses/LICENSE-2.0
* */
  private def copyRecursive(source: File, dest: File): Unit = {
    if (source.isDirectory) {
      if (!dest.mkdir()) {
        throw new IOException(s"Failed to create directory ${dest.getPath}")
      }
      val subfiles = source.listFiles()
      subfiles.foreach(f => copyRecursive(f, new File(dest, f.getName)))
    } else {
      Files.copy(source.toPath, dest.toPath)
    }
  }

  /*
  * This function is copied from Apache Spark source code located at https://github.com/apache/spark/blob/master/core/src/main/scala/org/apache/spark/util/Utils.scala
  * Copy of the apache license can be obtained from http://www.apache.org/licenses/LICENSE-2.0
  * */
  /**
    * Download `in` to `tempFile`, then move it to `destFile`.
    *
    * If `destFile` already exists:
    *   - no-op if its contents equal those of `sourceFile`,
    *   - throw an exception if `fileOverwrite` is false,
    *   - attempt to overwrite it otherwise.
    *
    * @param url URL that `sourceFile` originated from, for logging purposes.
    * @param in InputStream to download.
    * @param destFile File path to move `tempFile` to.
    * @param fileOverwrite Whether to delete/overwrite an existing `destFile` that does not match
    *                      `sourceFile`
    */
  private def downloadFile(
                            url: String,
                            in: InputStream,
                            destFile: File,
                            fileOverwrite: Boolean): Unit = {
    val logger = LogManager.getLogger("DownloadFile")
    val tempFile = File.createTempFile("fetchFileTemp", null,
      new File(destFile.getParentFile.getAbsolutePath))
    logger.info(s"Fetching $url to $tempFile")

    try {
      val out = new FileOutputStream(tempFile)
      copyStream(in, out, closeStreams = true)
      copyFile(url, tempFile, destFile, fileOverwrite, removeSourceFile = true)
    } finally {
      // Catch-all for the couple of cases where for some reason we didn't move `tempFile` to
      // `destFile`.
      if (tempFile.exists()) {
        tempFile.delete()
      }
    }
  }

 /*
* This function is copied from Apache Spark source code located at https://github.com/apache/spark/blob/master/core/src/main/scala/org/apache/spark/util/Utils.scala
* Copy of the apache license can be obtained from http://www.apache.org/licenses/LICENSE-2.0
* */
  def fetchHdfsFile(path: Path,
                     targetDir: java.io.File,
                     fs: FileSystem,
                     hadoopConf: Configuration,
                     fileOverwrite: Boolean,
                     filename: Option[String] = None): Unit = {
    if (!targetDir.exists() && !targetDir.mkdir()) {
      throw new IOException(s"Failed to create directory ${targetDir.getPath}")
    }
    val dest = new File(targetDir, filename.getOrElse(path.getName))
    if (fs.isFile(path)) {
      val in = fs.open(path)
      try {
        downloadFile(path.toString, in, dest, fileOverwrite)
      } finally {
        in.close()
      }
    } else {
      fs.listStatus(path).foreach { fileStatus =>
        fetchHdfsFile(fileStatus.getPath(), dest, fs, hadoopConf, fileOverwrite)
      }
    }
  }

  /*
  * Fetch file from HDFS using file URI and default Hadoop conf
  */
  def fetchHdfsFileByUri(uri: URI, 
                         targetDir: java.io.File,
                         fileOverwrite: Boolean,
                         filename: String): Unit = {
    val conf = getConf()
    val path = new Path(uri)
    val fs = path.getFileSystem(conf)
    fetchHdfsFile(path, targetDir, fs, conf, fileOverwrite, filename = Some(filename))
  }


  /*
 * This function is a modified version of Apache Spark source code located at https://github.com/apache/spark/blob/master/core/src/main/scala/org/apache/spark/util/Utils.scala
 * Copy of the apache license can be obtained from http://www.apache.org/licenses/LICENSE-2.0
 * */
  /**
    * Download a file or directory to target directory. Supports fetching the file in a variety of
    * ways, including HTTP, Hadoop-compatible filesystems, and files on a standard filesystem, based
    * on the URL parameter. Fetching directories is only supported from Hadoop-compatible
    * filesystems.
    * 'resolveStorageKey' param controls whether to retrieve the storage key from keyvault.
    * Throws SparkException if the target file already exists and has different contents than
    * the requested file.
    */
  def fetchFile(url: String,
                targetDir: java.io.File,
                filename: String,
                resolveStorageKey:Boolean=true): java.io.File = {
    val targetFile = new File(targetDir, filename)
    val uri = new URI(url)
    val fileOverwrite = false
    Option(uri.getScheme).getOrElse("file") match {
      case "file" =>
        // In the case of a local file, copy the local file to the target directory.
        // Note the difference between uri vs url.
        val sourceFile = if (uri.isAbsolute) new File(uri) else new File(url)
        copyFile(url, sourceFile, targetFile, fileOverwrite)
      case "abfs" | "abfss" => fetchHdfsFileByUri(uri, targetFile, fileOverwrite, filename)
      case "wasb" | "wasbs" =>
        if(resolveStorageKey) {
          resolveStorageAccountKeyForPath(url)
        }

        fetchHdfsFileByUri(uri, targetFile, fileOverwrite, filename)
      case other =>
        throw new EngineException(s"unsupported file paths with '$other' scheme")
    }

    targetFile
  }
}
