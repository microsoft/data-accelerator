package datax.test.app

import datax.app.BatchApp
import datax.test.testutils.SparkSessionTestWrapper
import org.json4s.jackson.JsonMethods.render
import org.scalatest.PrivateMethodTester
import org.scalatest.flatspec.AnyFlatSpec
import org.scalatest.matchers.should.Matchers

import java.util.TimeZone

class BatchAppTests extends AnyFlatSpec with Matchers with PrivateMethodTester with SparkSessionTestWrapper {

  TimeZone.setDefault(TimeZone.getTimeZone("GMT"))

  lazy val test1Spark = createTestSparkSession()
  lazy val test1InputDir = "/datax/tests/test1/input"
  lazy val test1OutputDir = "/datax/tests/test1/output"

  "BatchApp" should "process correctly a single blob" in {
    copyDirectoryToFs("datax/tests/test1", test1InputDir)
    cleanupDirectory(test1OutputDir)
    setEnv("process_start_datetime", "2023-10-03T00:00:00Z")
    setEnv("process_end_datetime", "2023-10-03T00:59:59Z")
    BatchApp.create(Array(
      "conf=datax/tests/test1/input/test.conf",
      "partition=false",
      "batchflushmetricsdelayduration=1 seconds",
      "filterTimeRange=false"), test1Spark)
    assert(fileExists(test1OutputDir + "/0.json"))
    val jsonVal = readFile(test1OutputDir + "/0.json")
    implicit val formats = org.json4s.DefaultFormats
    val output = org.json4s.jackson
      .parseJson(jsonVal)
    val nameValue = render(output \\ "Raw" \\ "name").extract[String]
    assert("hi" == nameValue)
  }
}
