package datax.test.config

import datax.config.ConfigManager.{initSparkConf, readConfigFileAsBase64Encoded}
import org.scalatest.PrivateMethodTester
import org.scalatest.flatspec.AnyFlatSpec
import org.scalatest.matchers.should.Matchers

class ConfigManagerTests extends AnyFlatSpec with Matchers with PrivateMethodTester {

  "initSparkConf" should "handle list of input arguments and fill in the global spark configuration object" in {
    val conf = initSparkConf(Array("spark.settingA=4", "otherconf.sub=3"))
    assert(conf.contains("spark.settingA"))
    assert(!conf.contains("otherconf.sub"))
    assert(conf.getInt("spark.settingA", 0) == 4)
  }

  "readConfigFileAsBase64Encoded" should "should return None if input is not base64 encoded" in {
    val content = readConfigFileAsBase64Encoded("abfss://A@example.dfs.core.windows.net/B/blob.json")
    assert(content.isEmpty)
  }

  "readConfigFileAsBase64Encoded" should "successfully transform encoded input into plain text in an array" in {
    val content = readConfigFileAsBase64Encoded("X19EYXRhWF9GaWxlSW5mbw0KX19EYXRhWE1ldGFkYXRhX091dHB1dFBhcnRpdGlvblRpbWU=")
    assert(content.isDefined)
    assert(content.get.sameElements(Array("__DataX_FileInfo", "__DataXMetadata_OutputPartitionTime")))
  }
}
