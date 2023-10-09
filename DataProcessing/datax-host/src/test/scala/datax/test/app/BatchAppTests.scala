package datax.test.app

import datax.app.BatchApp
import datax.test.testutils.SparkSessionTestWrapper
import org.apache.commons.io.FileUtils
import org.json4s.jackson.JsonMethods.render
import org.scalatest.PrivateMethodTester
import org.scalatest.flatspec.AnyFlatSpec
import org.scalatest.matchers.should.Matchers
import org.json4s.jackson.parseJson

import java.time.Instant
import java.util.TimeZone

class BatchAppTests extends AnyFlatSpec with Matchers with PrivateMethodTester with SparkSessionTestWrapper {

  case class TestCase(inputArgs: Array[String], envVars: Array[(String, String)], blobs: Array[(String, String)], projectionData: String, transformData: String, schemaData: String) {
    lazy val Timestamp = Instant.now().toEpochMilli
    lazy val Directory = FileUtils.getTempDirectory
    lazy val InputDir = s"${Directory.getPath}/datax/${Timestamp}/input"
    lazy val OutputDir = s"${Directory.getPath}/datax/${Timestamp}/output"

    envVars.foreach(envVar => setEnv(envVar._1, envVar._2))
    writeBlobs()

    def getConfig(): String = {
      s"""
         |datax.job.name=test
         |datax.job.input.default.blobschemafile=${encodeToBase64String(schemaData)}
         |datax.job.input.default.blobpathregex=.*/input/(\\d{4})/(\\d{2})/(\\d{2})/(\\d{2})/.*$$
         |datax.job.input.default.filetimeregex=(\\d{4}/\\d{2}/\\d{2}/\\d{2})$$
         |datax.job.input.default.sourceidregex=file:/.*/(input)/.*
         |datax.job.input.default.blob.test.path=${InputDir}/{yyyy/MM/dd/HH}/
         |datax.job.input.default.blob.test.partitionincrement=1
         |datax.job.input.default.blob.test.compressiontype=none
         |datax.job.input.default.source.input.target=output
         |datax.job.output.default.blob.group.main.folder=${OutputDir}
         |datax.job.output.default.blob.compressiontype=none
         |datax.job.process.transform=${encodeToBase64String(transformData)}
         |datax.job.process.projection=${encodeToBase64String(projectionData)}
         |""".stripMargin
    }

    def writeBlobs(): Unit = {
      var i = 0
      blobs.foreach(blobData => blobData._2.trim().split("\n").foreach(blob => {
        writeFile(s"${InputDir}/${blobData._1}/blob_$i.json", blob.trim())
        i += 1
      }))
    }

    def getInputArgs(): Array[String] = {
      Array(
        "spark.master=local",
        "spark.hadoop.fs.file.impl=com.globalmentor.apache.hadoop.fs.BareLocalFileSystem",
        "spark.hadoop.fs.azure.test.emulator=true",
        "spark.hadoop.fs.azure.storage.emulator.account.name=devstoreaccount1.blob.windows.core.net",
        s"conf=${encodeToBase64String(getConfig())}") ++ inputArgs
    }

    def readBlob(blobFileName: String): String = {
      val blobFullPath = s"$OutputDir/$blobFileName"
      assert(fileExists(blobFullPath))
      val jsonVal = readFile(blobFullPath)
      jsonVal
    }
  }

  TimeZone.setDefault(TimeZone.getTimeZone("GMT"))

  lazy val test1 = TestCase(
    blobs = Array(
      ("2023/10/03/00", """
        |{ "name": "hello" }
        |""".stripMargin),
      ("2023/10/03/00", """
        |{ "name": "world" }
        |""".stripMargin)),
    projectionData =
      """
        |__DataX_FileInfo
        |__DataXMetadata_OutputPartitionTime
        |Raw
        |DataXProperties
        |""".stripMargin,
    transformData =
      """
        |default = SELECT * FROM DataXProcessedInput
        |""".stripMargin,
    schemaData =
      """
        |{
        |  "type": "struct",
        |  "fields": [
        |    {
        |      "name": "name",
        |      "type": "string",
        |      "nullable": true,
        |      "metadata": {}
        |    }
        |  ]
        |}
        |""".stripMargin,
    inputArgs = Array(
      "partition=true",
      "batchflushmetricsdelayduration=1 seconds",
      "filterTimeRange=false"
    ),
    envVars = Array(
      ("process_start_datetime", "2023-10-03T00:00:00Z"),
      ("process_end_datetime", "2023-10-03T01:59:59Z")
    )
  )
  "BatchApp" should "process correctly a single blob running in local machine" in {
    BatchApp.main(test1.getInputArgs())
    implicit val formats = org.json4s.DefaultFormats
    assert("hello" == render(parseJson(test1.readBlob("0.json")) \\ "Raw" \\ "name").extract[String])
    assert("world" == render(parseJson(test1.readBlob("1.json")) \\ "Raw" \\ "name").extract[String])
  }
}
