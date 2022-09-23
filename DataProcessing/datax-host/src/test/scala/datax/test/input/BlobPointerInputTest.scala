package datax.test.input

import datax.input.BlobPointerInput
import org.scalatest.{FlatSpec, Matchers, PrivateMethodTester}

import java.sql.Timestamp

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

  "BlobPointerInput.extractTimeFromBlobPath" should "work with blob path" in {
    val blobPath = s"""abfss://containername@storageacct.dfs.core.windows.net/root/0/y=2022/m=09/d=22/h=05/m=40/PT5M.json"""
    val regex = "(y=\\d{4}/m=\\d{2}/d=\\d{2}/h=\\d{2}/m=\\d{2})/PT5M.json"
    val extractTimeFromBlobPath = PrivateMethod[Timestamp]('extractTimeFromBlobPath)
    val result = BlobPointerInput invokePrivate extractTimeFromBlobPath(blobPath, regex.r, "'y'=yyyy/'m'=MM/'d'=dd/'h'=HH/'m'=mm")
    assert(result.toString === "2022-09-22 05:40:00.0")

  }
}
