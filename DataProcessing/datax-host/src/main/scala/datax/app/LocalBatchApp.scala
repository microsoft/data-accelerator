package datax.app

import datax.config.UnifiedConfig
import datax.fs.HadoopClient
import datax.host.SparkSessionSingleton
import datax.processor.BatchBlobProcessor
import org.apache.commons.io.FileUtils
import org.apache.commons.lang3.StringUtils
import org.apache.hadoop.fs.FileStatus

import java.io.File
import java.time.Instant
import java.util.Base64.getEncoder
import java.util.TimeZone

trait EnvUtils {
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
}

trait SparkUtils extends EnvUtils {
  /**
   * Converts a environment variable into its spark representation
   *
   * @param envVarName  The environment variable name
   * @param envVarValue The environment variable value
   * @return
   */
  def ToSparkProps(envVarName: String, envVarValue: String): Array[String] = {
    Array(s"spark.yarn.appMasterEnv.${envVarName}=${envVarValue}",
      s"spark.executorEnv.${envVarName}=${envVarValue}")
  }
}

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

  def getWorkingDirectory: String

  def getInputDirectory: String

  def getOutputDirectory: String

  def getFiles(path: String): Iterator[FileStatus]
}

/**
 * Local File system implementation
 */
case class LocalAppLocalFileSystem() extends LocalAppFileSystem {
  val Timestamp: String = Instant.now().toEpochMilli.toString
  val Directory: String = FileUtils.getTempDirectory.getPath
  val WorkingDir = s"${Directory}/datax/${Timestamp}"
  val InputDir = s"${WorkingDir}/input"
  val OutputDir = s"${WorkingDir}/output"

  def fileExists(fsFileName: String): Boolean = {
    new File(fsFileName).exists()
  }

  def readFile(fsFileName: String): String = {
    FileUtils.readFileToString(new File(fsFileName))
  }

  def writeFile(path: String, data: String): Unit = {
    FileUtils.write(new File(path), data)
  }

  def getFiles(path: String): Iterator[FileStatus] = {
    HadoopClient.listFileObjects(path)
  }

  def getWorkingDirectory(): String = WorkingDir

  def getInputDirectory(): String = InputDir

  def getOutputDirectory(): String = OutputDir
}

/**
 * Configuration supplied to input parameter -conf
 */
trait ConfigGenerator {
  def getAppName: String
  def getEnvPrefix: String
  def getConfig: LocalAppFileSystem => AppConfiguration
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
  def Value: String = encodeValue(configData.stripMargin.trim())
}

/**
 * Configuration supplied as a file
 * @param configFilePath The configuration file path
 */
case class FileConfigSource(configFilePath: String) extends ConfigSource with ConfigValueEncoder {
  def Value: String = configFilePath
}

case class MapConfigSource(configData: Map[String, String]) extends ConfigSource with ConfigValueEncoder {
  def Value: String = encodeValue(configData.map(kv => s"${kv._1}=${kv._2}").mkString("\n"))
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

trait AppConfiguration {
  def getConfig: Option[ConfigSource]
  def getInputArguments: Array[String]
  def getEnvironmentVariables: Array[(String, String)]
  def getBlobs: Array[SourceBlob]
  def getAdditionalFiles: Array[InputFileSource]
}

case class ValueAppConfiguration(configData: String,
                                 inputArguments: Array[String] = Array(),
                                 environmentVariables: Array[(String, String)] = Array(),
                                 blobs: Array[SourceBlob] = Array(),
                                 additionalFiles: Array[InputFileSource] = Array()) extends AppConfiguration {
  override def getConfig: Option[ConfigSource] = Some(ValueConfigSource(configData))

  override def getInputArguments: Array[String] = inputArguments

  override def getEnvironmentVariables: Array[(String, String)] = environmentVariables

  override def getBlobs: Array[SourceBlob] = blobs

  override def getAdditionalFiles: Array[InputFileSource] = additionalFiles
}


/**
 * The configuration is supplied via string values
 * @param jobName The job name
 * @param projectionData The projection data file content
 * @param transformData The transform data file content
 * @param schemaData The schema data file content
 * @param additionalSettings Additional properties from conf files
 */
case class LocalConfigGenerator(jobName: String,
                                startTime: String,
                                endTime: String,
                                projectionData: ConfigSource,
                                transformData: ConfigSource,
                                schemaData: ConfigSource,
                                additionalSettings: LocalAppFileSystem => Map[String, String] = _ => Map(),
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
                                aiAppenderBatchDate: String = "",
                                inputArgs: Array[String] = Array(),
                                envVars: Array[(String, String)] = Array(),
                                blobs: Array[SourceBlob] = Array(),
                                additionalFiles: Array[InputFileSource] = Array()
                           ) extends ConfigGenerator with SparkUtils {
  def getEnvPrefix = envPrefix
  def getAppName = appName
  def getDefaultVaultName: String = defaultVaultName
  def getAppInsightsKeyRef: String = appInsightsKeyRef
  def getAIAppenderEnabled: String = aiAppenderEnabled
  def getAIAppenderBatchDate: String = aiAppenderBatchDate
  def getBlobWriterTimeout: String = blobWriterTimeout
  def getConfig: LocalAppFileSystem => AppConfiguration = {
    fs => new AppConfiguration {
      override def getConfig: Option[ConfigSource] = Some(MapConfigSource(
        Map(s"${envPrefix.toLowerCase}.job.name" -> s"$jobName",
            s"${envPrefix.toLowerCase}.job.input.default.blobschemafile" -> s"${schemaData.Value}",
            s"${envPrefix.toLowerCase}.job.input.default.blobpathregex" -> s"$blobPathRegex",
            s"${envPrefix.toLowerCase}.job.input.default.filetimeregex" -> s"$fileTimeRegex",
            s"${envPrefix.toLowerCase}.job.input.default.sourceidregex" -> s"$sourceIdRegex",
            s"${envPrefix.toLowerCase}.job.input.default.filetimeformat" -> s"$fileTimeFormat",
            s"${envPrefix.toLowerCase}.job.input.default.source.input.target" -> "output",
            s"${envPrefix.toLowerCase}.job.input.default.blob.input.path" -> s"${fs.InputDir}/$blobInputPath/",
            s"${envPrefix.toLowerCase}.job.input.default.blob.input.partitionincrement" -> s"$inputPartitionIncrement",
            s"${envPrefix.toLowerCase}.job.input.default.blob.input.compressiontype" -> s"$inputCompressionType",
            s"${envPrefix.toLowerCase}.job.output.default.blob.compressiontype" -> s"$outputCompressionType",
            s"${envPrefix.toLowerCase}.job.output.default.blob.group.main.folder" -> s"${fs.OutputDir}/$outputPartition",
            s"${envPrefix.toLowerCase}.job.process.transform" -> s"${transformData.Value}",
            s"${envPrefix.toLowerCase}.job.process.projection" -> s"${projectionData.Value}")
              ++ additionalSettings(fs)))

      override def getInputArguments: Array[String] = {
        val inputArgsPlusEnvVarsArgs = inputArgs ++ getEnvironmentVariables.flatMap(envPair => ToSparkProps(envPair._1, envPair._2))
        val partition = inputArgsPlusEnvVarsArgs.find(arg => arg.startsWith("partition=")).getOrElse("partition=true")
        val filterTimeRange = inputArgsPlusEnvVarsArgs.find(arg => arg.startsWith("filterTimeRange=")).getOrElse("filterTimeRange=false")
        Array(partition, filterTimeRange) ++ inputArgsPlusEnvVarsArgs
      }

      override def getEnvironmentVariables: Array[(String, String)] = {
        envVars ++ (if (StringUtils.isNotEmpty(getEnvPrefix)) {
          Array((s"DATAX_NAMEPREFIX", getEnvPrefix))
        } else {
          Array[(String, String)]()
        }) ++
          (if (StringUtils.isNotEmpty(getAppName)) {
            Array((s"${getEnvPrefix.toUpperCase()}_APPNAME", getAppName))
          } else {
            Array[(String, String)]()
          }) ++
          (if (StringUtils.isNotEmpty(getBlobWriterTimeout)) {
            Array((s"${getEnvPrefix.toUpperCase()}_BlobWriterTimeout", getBlobWriterTimeout))
          } else {
            Array[(String, String)]()
          }) ++
          (if (StringUtils.isNotEmpty(getDefaultVaultName)) {
            Array((s"${getEnvPrefix.toUpperCase()}_DEFAULTVAULTNAME", getDefaultVaultName))
          } else {
            Array[(String, String)]()
          }) ++
          (if (StringUtils.isNotEmpty(getAppInsightsKeyRef)) {
            Array((s"${getEnvPrefix.toUpperCase()}_APPINSIGHTKEYREF", getAppInsightsKeyRef))
          } else {
            Array[(String, String)]()
          }) ++
          (if (StringUtils.isNotEmpty(getAIAppenderEnabled)) {
            Array((s"${getEnvPrefix.toUpperCase()}_AIAPPENDERENABLED", getAIAppenderEnabled))
          } else {
            Array[(String, String)]()
          }) ++
          (if (StringUtils.isNotEmpty(getAIAppenderBatchDate)) {
            Array((s"${getEnvPrefix.toUpperCase()}_AIAPPENDERBATCHDATE", getAIAppenderBatchDate))
          } else {
            Array[(String, String)]()
          }) ++ Array(("process_start_datetime", getStartTime)) ++ Array(("process_end_datetime", getEndTime))
      }

      override def getBlobs: Array[SourceBlob] = blobs

      override def getAdditionalFiles: Array[InputFileSource] = additionalFiles
    }
  }

  def getStartTime: String = startTime

  def getEndTime: String = endTime
}
trait LocalSparkApp {
  val LocalModeInputArgs = Array(
    "spark.master=local",
    "spark.hadoop.fs.file.impl=com.globalmentor.apache.hadoop.fs.BareLocalFileSystem",
    "spark.hadoop.fs.azure.test.emulator=true",
    "spark.hadoop.fs.azure.storage.emulator.account.name=devstoreaccount1.blob.windows.core.net")
}

/**
 * Represents a BatchApp that is managed and configured to be run in the local machine along
 * @param configuration The configuration supplied as a set of values
 * @param fs            The file system used by the local batch app, default is the local file system of the running local machine
 */
class LocalBatchApp(fs: LocalAppFileSystem,
                    configuration: Option[LocalAppFileSystem => AppConfiguration],
                    processor: Option[UnifiedConfig => BatchBlobProcessor]
                   ) extends EnvUtils with SparkUtils with LocalSparkApp {

  def getAppConfiguration = configuration.map(conf => conf(fs))

  /**
   * Writes the value supplied blobs into the backed up file system
   */
  def writeBlobs(): Unit = {
    val appConfiguration = getAppConfiguration
    appConfiguration.foreach(appConfiguration => {
      appConfiguration.getBlobs.foreach(blob => {
        fs.writeFile(s"${fs.InputDir}/${blob.getPartition}/${blob.getBlobName}", blob.getBlob(fs))
      })
    })
  }

  /**
   * Deploys additional files configured by the caller, that are copied into the workspace
   */
  def deployAdditionalFiles(): Unit = {
    val appConfiguration = getAppConfiguration
    appConfiguration.foreach(appConfiguration => {
      appConfiguration.getAdditionalFiles.foreach(file => {
        fs.writeFile(file.getFileName(fs), file.getFileData(fs))
      })
    })
  }

  /**
   * Expands the list of arguments on top of the caller ones to initialize local mode behavior and configuration generation
   * @return
   */
  def getInputArgs(): Array[String] = {
    val appConfiguration = getAppConfiguration
    val inputArguments = appConfiguration.map(conf => conf.getInputArguments).getOrElse(Array())
    val confValueArg = appConfiguration.flatMap(conf => conf.getConfig.map(conf => s"conf=${conf.Value}"))
    LocalModeInputArgs ++
      inputArguments ++
      confValueArg.map(value => Array(value)).getOrElse(Array())
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
   * Enumerate files in output folder
   * @return
   */
  def getOutputBlobFiles(): Iterator[FileStatus] = {
    fs.getFiles(fs.OutputDir)
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
   * Enumerate files at input folder
   * @return
   */
  def getInputBlobFiles(): Iterator[FileStatus] = {
    fs.getFiles(fs.InputDir)
  }

  def installEnvironmentVariables() = {
    val appConfiguration = getAppConfiguration
    appConfiguration.foreach(
      appConfiguration => appConfiguration.getEnvironmentVariables.foreach(
        envVar => setEnv(envVar._1, envVar._2
        )
      )
    )
  }

  /**
   * Entrypoint
   */
  def main(): Unit = {

    TimeZone.setDefault(TimeZone.getTimeZone("GMT"))
    // Install any env vars first
    installEnvironmentVariables()
    // Initialize workspace files
    writeBlobs()
    deployAdditionalFiles()
    // Reset spark session singleton
    SparkSessionSingleton.resetInstance()
    if(processor.isEmpty) {
      BatchApp.main(getInputArgs())
    }
    else {
      BatchApp.startWithProcessor(getInputArgs(), processor.get)
    }
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

  def apply(inputArgs: Array[String]) = {
    new LocalBatchApp(
      configuration = None,
      fs = LocalAppLocalFileSystem(),
      processor = None
    )
  }

  def apply(configuration: AppConfiguration): LocalBatchApp = {
    LocalBatchApp(configuration, None)
  }

  def apply(configuration: AppConfiguration, processor: Option[UnifiedConfig => BatchBlobProcessor]): LocalBatchApp = {
    new LocalBatchApp(
      fs = LocalAppLocalFileSystem(),
      configuration = Option(_ => configuration),
      processor = processor
    )
  }

  def apply(configuration: LocalConfigGenerator): LocalBatchApp = {
    LocalBatchApp(configuration, None)
  }

  def apply(configuration: LocalConfigGenerator, processor: Option[UnifiedConfig => BatchBlobProcessor]): LocalBatchApp = {
    new LocalBatchApp(
      fs = LocalAppLocalFileSystem(),
      configuration = Option(configuration.getConfig),
      processor = processor
    )
  }
}
