package datax.test.testutils

import org.apache.commons.io.FileUtils

import java.io.File
import java.nio.file.Files

trait SparkSessionTestWrapper {

  def setEnv(key: String, value: String) = {
    val field = System.getenv().getClass.getDeclaredField("m")
    field.setAccessible(true)
    val map = field.get(System.getenv()).asInstanceOf[java.util.Map[java.lang.String, java.lang.String]]
    map.put(key, value)
  }

  def fileExists(fsFileName: String): Boolean = {
    new File(fsFileName).exists()
  }

  def readFile(fsFileName: String): String = {
    FileUtils.readFileToString(new File(fsFileName))
  }

  def encodeToBase64String(src: String) = {
    new String(java.util.Base64.getEncoder.encode(src.getBytes()))
  }

  def writeFile(path: String, data: String) = {
    FileUtils.write(new File(path), data)
  }

}
