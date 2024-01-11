package datax.test.app

import datax.app.{LocalBatchApp, ValueConfigSource, ValueConfiguration, ValueSourceBlob}
import org.json4s.jackson.JsonMethods.render
import org.scalatest.PrivateMethodTester
import org.scalatest.flatspec.AnyFlatSpec
import org.scalatest.matchers.should.Matchers
import org.json4s.jackson.parseJson
import scala.collection.mutable

class BatchAppTests extends AnyFlatSpec with Matchers with PrivateMethodTester {
  
  lazy val testApp = LocalBatchApp(blobs = Array(
    ValueSourceBlob(partition = "2023/10/03/00", stamp = "20231003_002655", blobData =
      """
        |{ "name": "hello" }
        |""".stripMargin),
    ValueSourceBlob(partition = "2023/10/03/01", stamp = "20231003_012655", blobData =
      """
        |{ "name": "world" }
        |""".stripMargin),
    ValueSourceBlob(partition = "2023/10/03/02", stamp = "20231003_022655", blobData =
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
          |""".stripMargin)
    ))
  )
  "BatchApp" should "process correctly a single blob running in local machine" in {
    testApp.main()
    implicit val formats = org.json4s.DefaultFormats
    val results = testApp.getOutputBlobFiles().map(
      f => render(parseJson(testApp.readOutputBlob(f.getPath.getName, "2023/10/03/00")) \\ "Raw" \\ "name").extract[String]
    ).toList
    val expected = mutable.Queue[String]("hello", "world", "!")
    while(expected.nonEmpty) {
      val check = expected.dequeue()
      assert(results.contains(check))
    }
  }
}
