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
  def getEnvPrefix: String
  def getConfig(fs: LocalAppFileSystem): String
  def getStartTime: String
  def getEndTime: String
}

trait SourceBlob {
  def getBlob(fs: LocalAppFileSystem) : String
  def getPartition: String
  def getStamp: String
}

case class ValueSourceBlob(partition: String, blobData: String, stamp: String = "") extends SourceBlob {
  def getBlob(fs: LocalAppFileSystem) = blobData
  def getPartition = partition
  def getStamp = stamp
}

case class FileSourceBlob(partition: String, blobFilePath: String, stamp: String = "") extends SourceBlob {
  def getBlob(fs: LocalAppFileSystem): String = {
    fs.readFile(blobFilePath)
  }

  def getPartition: String = partition

  def getStamp = stamp
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
case class ValueConfiguration(jobName: String, startTime: String, endTime: String, projectionData: ConfigSource, transformData: ConfigSource, schemaData: ConfigSource, additionalSettings: LocalAppFileSystem => String = _ => "", prefix: String = "DataX", outputPartition: String = "") extends LocalConfiguration with ConfigValueEncoder {
  def getEnvPrefix = prefix
  def getConfig(fs: LocalAppFileSystem): String = {
    encodeValue(s"""
       |${prefix.toLowerCase}.job.name=$jobName
       |${prefix.toLowerCase}.job.input.default.blobschemafile=${schemaData.Value}
       |${prefix.toLowerCase}.job.input.default.blobpathregex=.*/input/(\\d{4})/(\\d{2})/(\\d{2})/(\\d{2})/.*$$
       |${prefix.toLowerCase}.job.input.default.filetimeregex=.*/input/\\d{4}/\\d{2}/\\d{2}/\\d{2}/(\\d{4}\\d{2}\\d{2}_\\d{2}\\d{2}\\d{2}).*$$
       |${prefix.toLowerCase}.job.input.default.sourceidregex=file:/.*/(input)/.*
       |${prefix.toLowerCase}.job.input.default.filetimeformat=yyyyMMdd_HHmmss
       |${prefix.toLowerCase}.job.input.default.source.input.target=output
       |${prefix.toLowerCase}.job.input.default.blob.input.path=${fs.InputDir}/{yyyy/MM/dd/HH}/
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
        fs.writeFile(s"${fs.InputDir}/${blob.getPartition}/${blob.getStamp}.blob", blob.getBlob(fs))
      })
    }
  }

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

    configuration.foreach(conf => {
      if(StringUtils.isNotEmpty(conf.getEnvPrefix)) {
        setEnv("DATAX_NAMEPREFIX", conf.getEnvPrefix)
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
