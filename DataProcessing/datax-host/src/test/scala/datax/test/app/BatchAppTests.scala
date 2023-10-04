package datax.test.app

import datax.app.BatchApp
import datax.test.testutils.SparkSessionTestWrapper
import org.scalatest.{FlatSpec, Matchers, PrivateMethodTester}

import java.util.TimeZone

class BatchAppTests extends FlatSpec with Matchers with PrivateMethodTester with SparkSessionTestWrapper {

  TimeZone.setDefault(TimeZone.getTimeZone("GMT"))

  org.apache.log4j.BasicConfigurator.configure()

  lazy val test1Spark = createTestSparkSession()

  "BatchApp" should "process correctly a single blob" in {
    copyDirectoryToFs("datax/tests/test1", "/test1/input")
    setEnv("process_start_datetime", "2023-10-03T00:00:00Z")
    setEnv("process_end_datetime", "2023-10-03T00:59:59Z")
    BatchApp.create(Array(
      "conf=/test1/input/test.conf",
      "partition=false",
      "batchflushmetricsdelayduration=1 seconds",
      "filterTimeRange=false"), test1Spark)
  }
}
