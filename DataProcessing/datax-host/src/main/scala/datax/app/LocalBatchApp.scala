package datax.app

import org.apache.commons.io.FileUtils
import org.apache.commons.lang3.StringUtils

import java.io.File
import java.time.Instant
import java.util.TimeZone

case class LocalBatchApp(inputArgs: Array[String], envVars: Array[(String, String)], blobs: Array[(String, String)], projectionData: String, transformData: String, schemaData: String, additionalSettings: String = "") {

  TimeZone.setDefault(TimeZone.getTimeZone("GMT"))

  lazy val Timestamp = Instant.now().toEpochMilli
  lazy val Directory = FileUtils.getTempDirectory
  lazy val InputDir = s"${Directory.getPath}/datax/${Timestamp}/input"
  lazy val OutputDir = s"${Directory.getPath}/datax/${Timestamp}/output"
  lazy val LocalModeInputArgs = Array(
    "spark.master=local",
    "spark.hadoop.fs.file.impl=com.globalmentor.apache.hadoop.fs.BareLocalFileSystem",
    "spark.hadoop.fs.azure.test.emulator=true",
    "spark.hadoop.fs.azure.storage.emulator.account.name=devstoreaccount1.blob.windows.core.net")

  envVars.foreach(envVar => setEnv(envVar._1, envVar._2))
  writeBlobs()

  def getConfig(): String = {
    s"""
       |datax.job.name=test
       |datax.job.input.default.blobschemafile=${encodeToBase64String(schemaData)}
       |datax.job.input.default.blobpathregex=.*/input/(\\d{4})/(\\d{2})/(\\d{2})/(\\d{2})/.*$$
       |datax.job.input.default.filetimeregex=(\\d{4}/\\d{2}/\\d{2}/\\d{2})$$
       |datax.job.input.default.sourceidregex=file:/.*/(input)/.*
       |datax.job.input.default.source.input.target=output
       |datax.job.input.default.blob.input.path=${InputDir}/{yyyy/MM/dd/HH}/
       |datax.job.output.default.blob.group.main.folder=${OutputDir}
       |datax.job.process.transform=${encodeToBase64String(transformData)}
       |datax.job.process.projection=${encodeToBase64String(projectionData)}
       |""".stripMargin.trim() + "\n" + additionalSettings.trim()
  }

  def writeBlobs(): Unit = {
    var i = 0
    blobs.foreach(blobData => blobData._2.trim().split("\n").foreach(blob => {
      writeFile(s"${InputDir}/${blobData._1}/blob_$i.json", blob.trim())
      i += 1
    }))
  }

  def getInputArgs(): Array[String] = {
    LocalModeInputArgs ++ Array(s"conf=${encodeToBase64String(getConfig())}") ++ inputArgs
  }

  def readOutputBlob(blobFileName: String, outputPartition: String = ""): String = {
    val blobFullPath = if(StringUtils.isEmpty(outputPartition)) s"$OutputDir/$blobFileName" else s"$OutputDir/$outputPartition/$blobFileName"
    assert(fileExists(blobFullPath))
    val jsonVal = readFile(blobFullPath)
    jsonVal
  }

  def setEnv(key: String, value: String) = {
    val field = System.getenv().getClass.getDeclaredField("m")
    field.setAccessible(true)
    val map = field.get(System.getenv()).asInstanceOf[java.util.Map[java.lang.String, java.lang.String]]
    map.put(key, value)
  }

  def fileExists(fsFileName: String): Boolean = {
    new File(fsFileName).exists()
  }

  def readFile(fsFileName: String): String = {
    FileUtils.readFileToString(new File(fsFileName))
  }

  def encodeToBase64String(src: String) = {
    new String(java.util.Base64.getEncoder.encode(src.getBytes()))
  }

  def writeFile(path: String, data: String) = {
    FileUtils.write(new File(path), data)
  }

  def main(inputArguments: Array[String] = Array.empty): Unit = {
    BatchApp.main(getInputArgs())
  }
}
