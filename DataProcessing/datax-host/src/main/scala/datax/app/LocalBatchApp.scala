package datax.app

import datax.host.SparkSessionSingleton
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

/**
 * Represents a blob that exists in the workspace
 */
trait SourceBlob {
  def getBlob(fs: LocalAppFileSystem) : String
  def getPartition: String
  def getStamp: String
  def getExtension: String
  def getBlobName: String = s"${getStamp}.${getExtension}"
}

/**
 * Defines a blob that is going to be generated within the LocalBatchApp workspace
 * @param partition The blob assigned partition within the workspace
 * @param blobData The blob content
 * @param stamp The assigned stamp to the created blob
 * @param extension The extension of the created blob
 */
case class ValueSourceBlob(partition: String, blobData: String, stamp: String = "", extension: String = "blob") extends SourceBlob {
  def getBlob(fs: LocalAppFileSystem): String = blobData
  def getPartition: String = partition
  def getStamp: String = stamp
  def getExtension: String = extension
}

/**
 * Reference to a blob that already exists in a fs and is copied within the LocalBatchApp workspace
 * @param partition The blob assigned partition within the workspace
 * @param blobFilePath The existing blob path
 * @param stamp The assigned stamp to the blob to be imported
 * @param extension The extension for the assigned blob
 */
case class FileSourceBlob(partition: String, blobFilePath: String, stamp: String = "", extension: String = "blob") extends SourceBlob {
  def getBlob(fs: LocalAppFileSystem): String = {
    fs.readFile(blobFilePath)
  }
  def getPartition: String = partition
  def getStamp: String = stamp
  def getExtension: String = extension
}

/**
 * Represents a configuration value piece
 */
trait ConfigSource {
  def Value: String
}

/**
 * Configuration supplied as base64 content
 * @param configData A base64 configuration file content
 */
case class ValueConfigSource(configData: String) extends ConfigSource with ConfigValueEncoder {
  def Value: String = encodeValue(configData)
}

/**
 * Configuration supplied as a file
 * @param configFilePath The configuration file path
 */
case class FileConfigSource(configFilePath: String) extends ConfigSource with ConfigValueEncoder {
  def Value: String = configFilePath
}

/**
 * Represents an extra file to be deployed in LocalBatchApp workspace
 */
trait InputFileSource {
  def getFileData(fs: LocalAppFileSystem): String
  def getFileName(fs: LocalAppFileSystem): String
}

/**
 * Represents a file source by providing the string value directly for deploy new files method
 * @param fileName The file name to use
 * @param fileContent The file contents
 */
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
                              envPrefix: String = "DataX",
                              outputPartition: String = "",
                              appName: String = "test",
                              inputCompressionType: String = "none",
                              outputCompressionType: String = "none",
                              inputPartitionIncrement: Int = 1,
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
  def getEnvPrefix = envPrefix
  def getAppName = appName
  def getDefaultVaultName: String = defaultVaultName
  def getAppInsightsKeyRef: String = appInsightsKeyRef
  def getAIAppenderEnabled: String = aiAppenderEnabled
  def getAIAppenderBatchDate: String = aiAppenderBatchDate
  def getBlobWriterTimeout: String = blobWriterTimeout
  def getConfig(fs: LocalAppFileSystem): String = {
    encodeValue(s"""
       |${envPrefix.toLowerCase}.job.name=$jobName
       |${envPrefix.toLowerCase}.job.input.default.blobschemafile=${schemaData.Value}
       |${envPrefix.toLowerCase}.job.input.default.blobpathregex=$blobPathRegex
       |${envPrefix.toLowerCase}.job.input.default.filetimeregex=$fileTimeRegex
       |${envPrefix.toLowerCase}.job.input.default.sourceidregex=$sourceIdRegex
       |${envPrefix.toLowerCase}.job.input.default.filetimeformat=$fileTimeFormat
       |${envPrefix.toLowerCase}.job.input.default.source.input.target=output
       |${envPrefix.toLowerCase}.job.input.default.blob.input.path=${fs.InputDir}/$blobInputPath/
       |${envPrefix.toLowerCase}.job.input.default.blob.input.partitionincrement=$inputPartitionIncrement
       |${envPrefix.toLowerCase}.job.input.default.blob.input.compressiontype=$inputCompressionType
       |${envPrefix.toLowerCase}.job.output.default.blob.compressiontype=$outputCompressionType
       |${envPrefix.toLowerCase}.job.output.default.blob.group.main.folder=${fs.OutputDir}/$outputPartition
       |${envPrefix.toLowerCase}.job.process.transform=${transformData.Value}
       |${envPrefix.toLowerCase}.job.process.projection=${projectionData.Value}
       |""".stripMargin.trim() + "\n" + additionalSettings(fs).trim())
  }

  def getStartTime: String = startTime

  def getEndTime: String = endTime
}

/**
 * Represents a BatchApp that is managed and configured to be run in the local machine along
 * @param inputArgs The list of input arguments
 * @param configuration The configuration supplied as a set of values
 * @param blobs A list of blobs to be copied within the local batch app workspace
 * @param envVars A list of key pairs to be deployed as environment variables
 * @param additionalFiles A list of additional files to be deployed within the local batch app workspace
 * @param fs The file system used by the local batch app, default is the local file system of the running local machine
 */
case class LocalBatchApp(inputArgs: Array[String] = Array.empty, configuration: Option[LocalConfiguration] = None, blobs: Array[SourceBlob] = Array(), envVars: Array[(String, String)] = Array(), additionalFiles: Array[InputFileSource] = Array.empty, fs: LocalAppFileSystem = LocalAppLocalFileSystem()) {

  lazy val LocalModeInputArgs = Array(
    "spark.master=local",
    "spark.hadoop.fs.file.impl=com.globalmentor.apache.hadoop.fs.BareLocalFileSystem",
    "spark.hadoop.fs.azure.test.emulator=true",
    "spark.hadoop.fs.azure.storage.emulator.account.name=devstoreaccount1.blob.windows.core.net")

  /**
   * Sets a environment variable programmatically
   *
   * @param key   The environment variable name
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
   * Converts a environment variable into its spark representation
   * @param envVarName The environment variable name
   * @param envVarValue The environment variable value
   * @return
   */
  def setEnvVarAndConvertToSparkProp(envVarName: String, envVarValue: String): Array[String] = {
    setEnv(envVarName, envVarValue)
    Array(s"spark.yarn.appMasterEnv.${envVarName}=${envVarValue}",
      s"spark.executorEnv.${envVarName}=${envVarValue}")
  }

  def getDefaultInputArgs(): Array[String] = {
    val partition = inputArgs.find(arg => arg.startsWith("partition=")).getOrElse("partition=true")
    val filterTimeRange = inputArgs.find(arg => arg.startsWith("filterTimeRange=")).getOrElse("filterTimeRange=false")
    val additionalEnvVars = {
      configuration.map(conf => {
        (if (StringUtils.isNotEmpty(conf.getEnvPrefix)) {
          setEnvVarAndConvertToSparkProp(s"DATAX_NAMEPREFIX", conf.getEnvPrefix)
        } else {
          Array[String]()
        }) ++
        (if (StringUtils.isNotEmpty(conf.getAppName)) {
          setEnvVarAndConvertToSparkProp(s"${conf.getEnvPrefix.toUpperCase()}_APPNAME", conf.getAppName)
        } else {
          Array[String]()
        }) ++
        (if (StringUtils.isNotEmpty(conf.getBlobWriterTimeout)) {
          setEnvVarAndConvertToSparkProp(s"${conf.getEnvPrefix.toUpperCase()}_BlobWriterTimeout", conf.getBlobWriterTimeout)
        } else {
          Array[String]()
        }) ++
        (if (StringUtils.isNotEmpty(conf.getDefaultVaultName)) {
          setEnvVarAndConvertToSparkProp(s"${conf.getEnvPrefix.toUpperCase()}_DEFAULTVAULTNAME", conf.getDefaultVaultName)
        } else {
          Array[String]()
        }) ++
        (if (StringUtils.isNotEmpty(conf.getAppInsightsKeyRef)) {
          setEnvVarAndConvertToSparkProp(s"${conf.getEnvPrefix.toUpperCase()}_APPINSIGHTKEYREF", conf.getAppInsightsKeyRef)
        } else {
          Array[String]()
        }) ++
        (if (StringUtils.isNotEmpty(conf.getAIAppenderEnabled)) {
          setEnvVarAndConvertToSparkProp(s"${conf.getEnvPrefix.toUpperCase()}_AIAPPENDERENABLED", conf.getAIAppenderEnabled)
        } else {
          Array[String]()
        }) ++
        (if (StringUtils.isNotEmpty(conf.getAIAppenderBatchDate)) {
          setEnvVarAndConvertToSparkProp(s"${conf.getEnvPrefix.toUpperCase()}_AIAPPENDERBATCHDATE", conf.getAIAppenderBatchDate)
        } else {
          Array[String]()
        }) ++ setEnvVarAndConvertToSparkProp("process_start_datetime", conf.getStartTime) ++ setEnvVarAndConvertToSparkProp("process_end_datetime", conf.getEndTime)
      })
    }.getOrElse(Array[String]())
    val envVarsInput = envVars.flatMap(envVar => setEnvVarAndConvertToSparkProp(envVar._1, envVar._2))
    Array(partition, filterTimeRange) ++ envVarsInput ++ additionalEnvVars
  }

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
      getDefaultInputArgs ++
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
   * Read a blob from the input directory
   * @param blobFileName The blob file name
   * @return
   */
  def readInputBlob(blobFileName: String): String = {
    val blobFullPath = s"${fs.InputDir}/$blobFileName"
    assert(fs.fileExists(blobFullPath))
    fs.readFile(blobFullPath)
  }

  /**
   * Entrypoint
   */
  def main(): Unit = {
    TimeZone.setDefault(TimeZone.getTimeZone("GMT"))
    if(!blobs.isEmpty) {
      writeBlobs()
    }
    if(!additionalFiles.isEmpty) {
      deployAdditionalFiles()
    }
    // Reset spark session singleton
    SparkSessionSingleton.resetInstance()
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
