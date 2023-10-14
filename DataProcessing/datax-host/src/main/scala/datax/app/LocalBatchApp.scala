package datax.app

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

  def getInputDirectory: String
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

  def getInputDirectory(): String = InputDir
}

/**
 * Configuration supplied to input parameter -conf
 */
trait LocalConfiguration {
  def getAppName: String
  def getEnvPrefix: String
  def getConfig(fs: LocalAppFileSystem): String
  def getStartTime: String
  def getEndTime: String
  def getBlobWriterTimeout: String
  def getDefaultVaultName: String
  def getAppInsightsKeyRef: String
  def getAIAppenderEnabled: String
  def getAIAppenderBatchDate: String
}

trait SourceBlob {
  def getBlob(fs: LocalAppFileSystem) : String
  def getPartition: String
  def getStamp: String
  def getExtension: String
  def getBlobName: String = s"${getStamp}.${getExtension}"
}

case class ValueSourceBlob(partition: String, blobData: String, stamp: String = "", extension: String = "blob") extends SourceBlob {
  def getBlob(fs: LocalAppFileSystem): String = blobData
  def getPartition: String = partition
  def getStamp: String = stamp
  def getExtension: String = extension
}

case class FileSourceBlob(partition: String, blobFilePath: String, stamp: String = "", extension: String = "blob") extends SourceBlob {
  def getBlob(fs: LocalAppFileSystem): String = {
    fs.readFile(blobFilePath)
  }
  def getPartition: String = partition
  def getStamp: String = stamp
  def getExtension: String = extension
}

trait ConfigSource {
  def Value: String
}

case class ValueConfigSource(configData: String) extends ConfigSource with ConfigValueEncoder {
  def Value: String = encodeValue(configData)
}

case class FileConfigSource(configFilePath: String) extends ConfigSource with ConfigValueEncoder {
  def Value: String = configFilePath
}

trait InputFileSource {
  def getFileData(fs: LocalAppFileSystem): String
  def getFileName(fs: LocalAppFileSystem): String
}

case class ValueInputFileSource (fileName: String, fileContent: String) extends InputFileSource {
  def getFileData(fs: LocalAppFileSystem): String = fileContent
  def getFileName(fs: LocalAppFileSystem): String = s"${fs.getInputDirectory}/$fileName"
}

/**
 * The configuration is supplied via string values
 * @param jobName The job name
 * @param projectionData The projection data file content
 * @param transformData The transform data file content
 * @param schemaData The schema data file content
 * @param additionalSettings Additional properties from conf files
 */
case class ValueConfiguration(jobName: String,
                              startTime: String,
                              endTime: String,
                              projectionData: ConfigSource,
                              transformData: ConfigSource,
                              schemaData: ConfigSource,
                              additionalSettings: LocalAppFileSystem => String = _ => "",
                              prefix: String = "DataX",
                              outputPartition: String = "",
                              appName: String = "test",
                              blobPathRegex: String = ".*/input/(\\d{4})/(\\d{2})/(\\d{2})/(\\d{2})/.*$",
                              fileTimeRegex: String = ".*/input/\\d{4}/\\d{2}/\\d{2}/\\d{2}/(\\d{4}\\d{2}\\d{2}_\\d{2}\\d{2}\\d{2}).*$",
                              sourceIdRegex: String = "file:/.*/(input)/.*",
                              fileTimeFormat: String = "yyyyMMdd_HHmmss",
                              blobInputPath: String = "{yyyy/MM/dd/HH}",
                              blobWriterTimeout: String = "60 seconds",
                              defaultVaultName: String = "",
                              appInsightsKeyRef: String = "",
                              aiAppenderEnabled: String = "false",
                              aiAppenderBatchDate: String = ""
                             ) extends LocalConfiguration with ConfigValueEncoder {
  def getEnvPrefix = prefix
  def getAppName = appName
  def getDefaultVaultName: String = defaultVaultName
  def getAppInsightsKeyRef: String = appInsightsKeyRef
  def getAIAppenderEnabled: String = aiAppenderEnabled
  def getAIAppenderBatchDate: String = aiAppenderBatchDate
  def getBlobWriterTimeout: String = blobWriterTimeout
  def getConfig(fs: LocalAppFileSystem): String = {
    encodeValue(s"""
       |${prefix.toLowerCase}.job.name=$jobName
       |${prefix.toLowerCase}.job.input.default.blobschemafile=${schemaData.Value}
       |${prefix.toLowerCase}.job.input.default.blobpathregex=$blobPathRegex
       |${prefix.toLowerCase}.job.input.default.filetimeregex=$fileTimeRegex
       |${prefix.toLowerCase}.job.input.default.sourceidregex=$sourceIdRegex
       |${prefix.toLowerCase}.job.input.default.filetimeformat=$fileTimeFormat
       |${prefix.toLowerCase}.job.input.default.source.input.target=output
       |${prefix.toLowerCase}.job.input.default.blob.input.path=${fs.InputDir}/$blobInputPath/
       |${prefix.toLowerCase}.job.output.default.blob.group.main.folder=${fs.OutputDir}/$outputPartition
       |${prefix.toLowerCase}.job.process.transform=${transformData.Value}
       |${prefix.toLowerCase}.job.process.projection=${projectionData.Value}
       |""".stripMargin.trim() + "\n" + additionalSettings(fs).trim())
  }

  def getStartTime: String = startTime

  def getEndTime: String = endTime
}

case class LocalBatchApp(inputArgs: Array[String], configuration: Option[LocalConfiguration] = None, blobs: Array[SourceBlob] = Array(), envVars: Array[(String, String)] = Array(), additionalFiles: Array[InputFileSource] = Array.empty, fs: LocalAppFileSystem = LocalAppLocalFileSystem()) {

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
      blobs.foreach(blob => {
        fs.writeFile(s"${fs.InputDir}/${blob.getPartition}/${blob.getBlobName}", blob.getBlob(fs))
      })
    }
  }

  /**
   * Deploys additional files configured by the caller, that are copied into the workspace
   */
  def deployAdditionalFiles(): Unit = {
    if(!additionalFiles.isEmpty) {
      additionalFiles.foreach(file => {
        fs.writeFile(file.getFileName(fs), file.getFileData(fs))
      })
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
   * Sets a environment variable programmatically
   * @param key The environment variable name
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
    configuration.foreach(conf => {
      if(StringUtils.isNotEmpty(conf.getEnvPrefix)) {
        setEnv(s"${conf.getEnvPrefix.toUpperCase()}_NAMEPREFIX", conf.getEnvPrefix)
      }
      if(StringUtils.isNotEmpty(conf.getAppName)) {
        setEnv(s"${conf.getEnvPrefix.toUpperCase()}_APPNAME", conf.getAppName)
      }
      if(StringUtils.isNotEmpty(conf.getBlobWriterTimeout)) {
        setEnv(s"${conf.getEnvPrefix.toUpperCase()}_BlobWriterTimeout", conf.getBlobWriterTimeout)
      }
      if(StringUtils.isNotEmpty(conf.getDefaultVaultName)) {
        setEnv(s"${conf.getEnvPrefix.toUpperCase()}_DEFAULTVAULTNAME", conf.getDefaultVaultName)
      }
      if(StringUtils.isNotEmpty(conf.getAppInsightsKeyRef)) {
        setEnv(s"${conf.getEnvPrefix.toUpperCase()}_APPINSIGHTKEYREF", conf.getAppInsightsKeyRef)
      }
      if(StringUtils.isNotEmpty(conf.getAIAppenderEnabled)) {
        setEnv(s"${conf.getEnvPrefix.toUpperCase()}_AIAPPENDERENABLED", conf.getAIAppenderEnabled)
      }
      if(StringUtils.isNotEmpty(conf.getAIAppenderBatchDate)) {
        setEnv(s"${conf.getEnvPrefix.toUpperCase()}_AIAPPENDERBATCHDATE", conf.getAIAppenderBatchDate)
      }
    })

    if(!blobs.isEmpty) {
      writeBlobs()
    }
    if(!additionalFiles.isEmpty) {
      deployAdditionalFiles()
    }
    configuration.foreach(conf => {
      setEnv("process_start_datetime", conf.getStartTime)
      setEnv("process_end_datetime", conf.getEndTime)
    })
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