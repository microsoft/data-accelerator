package datax.test.app

import datax.app.LocalBatchApp
import org.json4s.jackson.JsonMethods.render
import org.scalatest.PrivateMethodTester
import org.scalatest.flatspec.AnyFlatSpec
import org.scalatest.matchers.should.Matchers
import org.json4s.jackson.parseJson

class BatchAppTests extends AnyFlatSpec with Matchers with PrivateMethodTester {

  lazy val testApp = LocalBatchApp(
    blobs = Array(
      ("2023/10/03/00", """
        |{ "name": "hello" }
        |""".stripMargin),
      ("2023/10/03/01", """
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
    ),
    additionalSettings =
      """
        |datax.job.input.default.blob.input.partitionincrement=1
        |datax.job.input.default.blob.input.compressiontype=none
        |datax.job.output.default.blob.compressiontype=none
        |""".stripMargin
  )
  "BatchApp" should "process correctly a single blob running in local machine" in {
    testApp.main()
    implicit val formats = org.json4s.DefaultFormats
    assert(Array("hello", "world").contains(render(parseJson(testApp.readOutputBlob("0.json")) \\ "Raw" \\ "name").extract[String]))
    assert(Array("hello", "world").contains(render(parseJson(testApp.readOutputBlob("1.json")) \\ "Raw" \\ "name").extract[String]))
  }
}
