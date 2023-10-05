package datax.test.app

import datax.app.BatchApp
import datax.test.testutils.SparkSessionTestWrapper
import org.json4s.jackson.JsonMethods.render
import org.scalatest.PrivateMethodTester
import org.scalatest.flatspec.AnyFlatSpec
import org.scalatest.matchers.should.Matchers

import java.time.Instant
import java.util.TimeZone

class BatchAppTests extends AnyFlatSpec with Matchers with PrivateMethodTester with SparkSessionTestWrapper {

  TimeZone.setDefault(TimeZone.getTimeZone("GMT"))

  lazy val test1InputDir = "/datax/tests/test1/input"
  lazy val test1OutputDir = s"/datax/tests/test1/${Instant.now().toEpochMilli}/output"
  lazy val test1Projection =
    """
      |__DataX_FileInfo
      |__DataXMetadata_OutputPartitionTime
      |Raw
      |DataXProperties
      |""".stripMargin
  lazy val test1Transform =
    """
      |default = SELECT * FROM DataXProcessedInput
      |""".stripMargin
  lazy val test1Schema =
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
      |""".stripMargin
  lazy val test1Config =
    s"""
      |datax.job.name=test
      |datax.job.input.default.blobschemafile=${encodeToBase64String(test1Schema)}
      |datax.job.input.default.blobpathregex=.*/blobs/\\d{4}/\\d{2}/\\d{2}/\\d{2}/blob.json$$
      |datax.job.input.default.filetimeregex=(\\d{4}/\\d{2}/\\d{2}/\\d{2})$$
      |datax.job.input.default.blob.test.path=/datax/tests/test1/input/blobs/{yyyy/MM/dd/HH}/
      |datax.job.input.default.blob.test.partitionincrement=1
      |datax.job.input.default.blob.test.compressiontype=none
      |datax.job.input.default.source.testinput.target=testoutput
      |datax.job.output.default.blob.group.main.folder=${test1OutputDir}
      |datax.job.output.default.blob.compressiontype=none
      |datax.job.process.transform=${encodeToBase64String(test1Transform)}
      |datax.job.process.projection=${encodeToBase64String(test1Projection)}
      |""".stripMargin
  "BatchApp" should "process correctly a single blob running in local machine" in {
    copyDirectoryToFs("datax/tests/test1", test1InputDir)
    setEnv("process_start_datetime", "2023-10-03T00:00:00Z")
    setEnv("process_end_datetime", "2023-10-03T00:59:59Z")
    BatchApp.main(Array(
      "spark.master=local",
      "spark.hadoop.fs.file.impl=com.globalmentor.apache.hadoop.fs.BareLocalFileSystem",
      "spark.hadoop.fs.azure.test.emulator=true",
      "spark.hadoop.fs.azure.storage.emulator.account.name=devstoreaccount1.blob.windows.core.net",
      s"conf=${encodeToBase64String(test1Config)}",
      "partition=false",
      "batchflushmetricsdelayduration=1 seconds",
      "filterTimeRange=false"))
    assert(fileExists(test1OutputDir + "/0.json"))
    val jsonVal = readFile(test1OutputDir + "/0.json")
    implicit val formats = org.json4s.DefaultFormats
    val output = org.json4s.jackson
      .parseJson(jsonVal)
    val nameValue = render(output \\ "Raw" \\ "name").extract[String]
    assert("hi" == nameValue)
  }
}
