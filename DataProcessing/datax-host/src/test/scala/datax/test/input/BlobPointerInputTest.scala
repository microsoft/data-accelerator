package datax.test.input

import datax.input.BlobPointerInput
import org.scalatest.{FlatSpec, Matchers, PrivateMethodTester}

class BlobPointerInputTest extends FlatSpec with Matchers with PrivateMethodTester{
  org.apache.log4j.BasicConfigurator.configure()

  "BlobPointerInput class" should "initialize correctly" in {
    BlobPointerInput
  }

  "BlobPointerInput.extractSourceId" should "get SourceId with empty regex arg for wasbs" in {
    val bpi = BlobPointerInput
    val blobPath = s"""wasbs://containername@storageacct.blob.core.windows.net/prodX/Event/2022-01-01/02/1234567890.blob"""
    val extractSourceId = PrivateMethod[String]('extractSourceId)
    assert("storageacct" === (bpi invokePrivate extractSourceId(blobPath, "")))
  }

  "BlobPointerInput.extractSourceId" should "get SourceId with empty regex arg for abfss" in {
    val bpi = BlobPointerInput
    val blobPath = s"""abfss://containername@storageacct.dfs.core.windows.net/prodX/Event/2022-01-01/02/1234567890.blob"""
    val extractSourceId = PrivateMethod[String]('extractSourceId)
    assert("storageacct" === (bpi invokePrivate extractSourceId(blobPath, "")))
  }

  "BlobPointerInput.extractSourceId" should "get SourceId with regex argument" in {
    val bpi = BlobPointerInput
    val blobPath = s"""wasbs://containername@storageacct.blob.core.windows.net/prodX/Event/2022-01-01/02/1234567890.blob"""
    val regex = s"""^wasbs://containername@([\\w\\d+]+)\\.blob\\.core\\.windows\\.net/prodX/Event/"""
    val extractSourceId = PrivateMethod[String]('extractSourceId)
    assert("storageacct" === (bpi invokePrivate extractSourceId(blobPath, regex)))
  }
}
