package datax.test.app

import com.globalmentor.apache.hadoop.fs.BareLocalFileSystem
import datax.app.BatchApp
import datax.test.testutils.SparkSessionTestWrapper
import org.apache.hadoop.fs.FileSystem
import org.scalatest.{FlatSpec, Matchers, PrivateMethodTester}

class BatchAppTests extends FlatSpec with Matchers with PrivateMethodTester with SparkSessionTestWrapper {

  org.apache.log4j.BasicConfigurator.configure()

  "app" should "app" in {
    setEnv("process_start_datetime", "2023-10-03T00:00:00Z")
    setEnv("process_end_datetime", "2023-10-03T00:59:59Z")
    BatchApp.create(Array(
      "conf=classpath:example.conf",
      "partition=false",
      "batchflushmetricsdelayduration=1 seconds",
      "filterTimeRange=false"), createTestSparkSession())
  }
}
