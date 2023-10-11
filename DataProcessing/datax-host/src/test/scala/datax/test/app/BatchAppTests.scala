package datax.test.app

import datax.app.{LocalBatchApp, ValueConfigSource, ValueConfiguration, ValueSourceBlob}
import org.json4s.jackson.JsonMethods.render
import org.scalatest.PrivateMethodTester
import org.scalatest.flatspec.AnyFlatSpec
import org.scalatest.matchers.should.Matchers
import org.json4s.jackson.parseJson

class BatchAppTests extends AnyFlatSpec with Matchers with PrivateMethodTester {

import scala.collection.mutable

  lazy val testApp = LocalBatchApp(blobs = Array(
    ValueSourceBlob("2023/10/03/00", "20231003_002655",
      """
        |{ "name": "hello" }
        |""".stripMargin),
    ValueSourceBlob("2023/10/03/01", "20231003_012655",
      """
        |{ "name": "world" }
        |""".stripMargin),
    ValueSourceBlob("2023/10/03/02", "20231003_022655",
      """
        |{ "name": "!" }
        |""".stripMargin)),
    configuration = Some(ValueConfiguration(
      jobName = "test",
      startTime = "2023-10-03T00:00:00Z",
      endTime = "2023-10-03T02:59:59Z",
      outputPartition = "%1$tY/%1$tm/%1$td/%1$tH",
      projectionData =
        ValueConfigSource("""
          |__DataX_FileInfo
          |__DataXMetadata_OutputPartitionTime
          |Raw
          |DataXProperties
          |""".stripMargin),
      transformData =
        ValueConfigSource("""
          |default = SELECT * FROM DataXProcessedInput
          |""".stripMargin),
      schemaData =
        ValueConfigSource("""
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
          |""".stripMargin),
      additionalSettings = _ =>
        """
          |datax.job.input.default.blob.input.partitionincrement=1
          |datax.job.input.default.blob.input.compressiontype=none
          |datax.job.output.default.blob.compressiontype=none
          |""".stripMargin
    )),
    inputArgs = Array(
      "partition=true",
      "batchflushmetricsdelayduration=1 seconds",
      "filterTimeRange=false"
    )
  )
  "BatchApp" should "process correctly a single blob running in local machine" in {
    testApp.main()
    implicit val formats = org.json4s.DefaultFormats
    val results = Array[String](
      render(parseJson(testApp.readOutputBlob("0.json", "2023/10/03/00")) \\ "Raw" \\ "name").extract[String],
      render(parseJson(testApp.readOutputBlob("1.json", "2023/10/03/00")) \\ "Raw" \\ "name").extract[String],
      render(parseJson(testApp.readOutputBlob("2.json", "2023/10/03/00")) \\ "Raw" \\ "name").extract[String]
    )
    val expected = mutable.Queue[String]("hello", "world", "!")
    while(expected.nonEmpty) {
      val check = expected.dequeue()
      assert(results.contains(check))
    }
  }
}
