package datax.app

import datax.utility.ArgumentsParser
import org.apache.commons.io.FileUtils
import org.apache.commons.lang3.StringUtils

import java.io.File
import java.time.Instant
import java.util.Base64.getEncoder
import java.util.TimeZone

/**
 * Encoder for configuration string supplied values
 */
trait ConfigValueEncoder {
  def encodeValue(src: String): String = {
    new String(getEncoder.encode(src.getBytes()))
  }
}

/**
 * File system that supports the Local App
 */
trait LocalAppFileSystem {
  val InputDir: String
  val OutputDir: String

  def fileExists(fsFileName: String): Boolean

  def readFile(fsFileName: String): String

  def writeFile(path: String, data: String): Unit
}

/**
 * Local File system implementation
 */
case class LocalAppLocalFileSystem() extends LocalAppFileSystem {
  val Timestamp: String = Instant.now().toEpochMilli.toString
  val Directory: String = FileUtils.getTempDirectory.getPath
  val InputDir = s"${Directory}/datax/${Timestamp}/input"
  val OutputDir = s"${Directory}/datax/${Timestamp}/output"

  def fileExists(fsFileName: String): Boolean = {
    new File(fsFileName).exists()
  }

  def readFile(fsFileName: String): String = {
    FileUtils.readFileToString(new File(fsFileName))
  }

  def writeFile(path: String, data: String): Unit = {
    FileUtils.write(new File(path), data)
  }
}

/**
 * Configuration supplied to input parameter -conf
 */
trait LocalConfiguration {
  def getConfig(fs: LocalAppFileSystem): String
}

trait SourceBlob {
  def getBlob(fs: LocalAppFileSystem) : String
  def getPartition: String
}

case class ValueSourceBlob(partition: String, blobData: String) extends SourceBlob {
  def getBlob(fs: LocalAppFileSystem) = blobData
  def getPartition = partition
}

case class FileSourceBlob(partition: String, blobFilePath: String) extends SourceBlob {
  def getBlob(fs: LocalAppFileSystem): String = {
    fs.readFile(blobFilePath)
  }

  def getPartition: String = partition
}

/**
 * The configuration is supplied via string values
 * @param jobName The job name
 * @param projectionData The projection data file content
 * @param transformData The transform data file content
 * @param schemaData The schema data file content
 * @param additionalSettings Additional properties from conf files
 */
case class ValueConfiguration(jobName: String, projectionData: String, transformData: String, schemaData: String, additionalSettings: String = "") extends LocalConfiguration with ConfigValueEncoder {
  def getConfig(fs: LocalAppFileSystem): String = {
    encodeValue(s"""
       |datax.job.name=$jobName
       |datax.job.input.default.blobschemafile=${encodeValue(schemaData)}
       |datax.job.input.default.blobpathregex=.*/input/(\\d{4})/(\\d{2})/(\\d{2})/(\\d{2})/.*$$
       |datax.job.input.default.filetimeregex=(\\d{4}/\\d{2}/\\d{2}/\\d{2})$$
       |datax.job.input.default.sourceidregex=file:/.*/(input)/.*
       |datax.job.input.default.source.input.target=output
       |datax.job.input.default.blob.input.path=${fs.InputDir}/{yyyy/MM/dd/HH}/
       |datax.job.output.default.blob.group.main.folder=${fs.OutputDir}
       |datax.job.process.transform=${encodeValue(transformData)}
       |datax.job.process.projection=${encodeValue(projectionData)}
       |""".stripMargin.trim() + "\n" + additionalSettings.trim())
  }
}

case class LocalBatchApp(inputArgs: Array[String], configuration: Option[LocalConfiguration] = None, blobs: Array[SourceBlob] = Array(), envVars: Array[(String, String)] = Array(), fs: LocalAppFileSystem = LocalAppLocalFileSystem()) {

  lazy val LocalModeInputArgs = Array(
    "spark.master=local",
    "spark.hadoop.fs.file.impl=com.globalmentor.apache.hadoop.fs.BareLocalFileSystem",
    "spark.hadoop.fs.azure.test.emulator=true",
    "spark.hadoop.fs.azure.storage.emulator.account.name=devstoreaccount1.blob.windows.core.net")

  /**
   * Writes the value supplied blobs into the backed up file system
   */
  def writeBlobs(): Unit = {
    if(!blobs.isEmpty) {
      var i = 0
      blobs.foreach(blobData => blobData.getBlob(fs).trim().split("\n").foreach(blob => {
        fs.writeFile(s"${fs.InputDir}/${blobData.getPartition}/blob_$i.json", blob.trim())
        i += 1
      }))
    }
  }

  /**
   * Expands the list of arguments on top of the caller ones to initialize local mode behavior and configuration generation
   * @return
   */
  def getInputArgs(): Array[String] = {
    LocalModeInputArgs ++
      (configuration.map(conf => Array(s"conf=${conf.getConfig(fs)}")).getOrElse(Array.empty)) ++
      inputArgs
  }

  /**
   * Read a blob from the output directory partition
   * @param blobFileName The blob file name
   * @param outputPartition The partition within the output directory
   * @return
   */
  def readOutputBlob(blobFileName: String, outputPartition: String = ""): String = {
    val blobFullPath = if(StringUtils.isEmpty(outputPartition)) s"${fs.OutputDir}/$blobFileName" else s"${fs.OutputDir}/$outputPartition/$blobFileName"
    assert(fs.fileExists(blobFullPath))
    fs.readFile(blobFullPath)
  }

  /**
   * Sets a environment variable programatically
   * @param key The enviroment variable name
   * @param value The environment variable value
   * @return
   */
  def setEnv(key: String, value: String) = {
    val field = System.getenv().getClass.getDeclaredField("m")
    field.setAccessible(true)
    val map = field.get(System.getenv()).asInstanceOf[java.util.Map[java.lang.String, java.lang.String]]
    map.put(key, value)
  }

  /**
   * Entrypoint
   */
  def main(): Unit = {
    TimeZone.setDefault(TimeZone.getTimeZone("GMT"))
    if(!envVars.isEmpty) {
      envVars.foreach(envVar => setEnv(envVar._1, envVar._2))
    }
    if(!blobs.isEmpty) {
      writeBlobs()
    }
    BatchApp.main(getInputArgs())
  }
}

/**
 * Companion object to expose a static main method
 */
object LocalBatchApp {
  /**
   * Entry point for a LocalBatchApp when called externally via class reference
   * @param inputArguments The input arguments array
   */
  def main(inputArguments: Array[String]): Unit = {
    LocalBatchApp(inputArgs = inputArguments).main()
  }
}
