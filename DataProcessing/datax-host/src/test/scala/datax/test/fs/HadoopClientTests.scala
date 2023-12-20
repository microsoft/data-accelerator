package datax.test.fs

import datax.fs.HadoopClient.{createTempFilePathUri, getConf}
import org.apache.hadoop.fs.Path
import org.apache.hadoop.fs.azure.NativeAzureFileSystem
import org.scalatest.PrivateMethodTester
import org.scalatest.flatspec.AnyFlatSpec
import org.scalatest.matchers.should.Matchers

import scala.language.postfixOps

class HadoopClientTests extends AnyFlatSpec with Matchers with PrivateMethodTester {

  "createTempFilePathUri" should "create a temporary file path for a abfss blob path" in {
    val path = new Path("abfss://A@example.dfs.core.windows.net/B/blob.json")
    val uri = path.toUri
    val fs = new NativeAzureFileSystem()
    val tempFilePath = createTempFilePathUri(fs, uri, path)
    assert(Option(tempFilePath) isDefined)
    assert(tempFilePath.getPath.startsWith("/_$tmpHdfsFolder$/"))
    assert(tempFilePath.getPath.endsWith("-blob.json"))
    assert(tempFilePath.toString.startsWith("abfss://A@example.dfs.core.windows.net/_$tmpHdfsFolder$/"))
    assert(tempFilePath.toString.endsWith("-blob.json"))
  }
}
