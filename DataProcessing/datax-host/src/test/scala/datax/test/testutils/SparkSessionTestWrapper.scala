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

  def getClasspathFile(filePath: String): File = {
    val resourceFilePath = filePath.substring(10)
    new File(getClass.getClassLoader.getResource(resourceFilePath).getFile)
  }

  def isClasspathFileUri(filePath: String) = filePath != null && filePath.nonEmpty && filePath.startsWith("classpath:")

  def cleanupDirectory(fsTargetFolder: String) = {
    val targetFolder = new File(fsTargetFolder)
    if (targetFolder.exists()) {
      FileUtils.cleanDirectory(targetFolder)
    }
  }

  def fileExists(fsFileName: String): Boolean = {
    new File(fsFileName).exists()
  }

  def readFile(fsFileName: String): String = {
    FileUtils.readFileToString(new File(fsFileName))
  }

  def copyDirectoryToFs(resourceSourceFolder: String, fsTargetFolder: String, cleanupTarget: Boolean = true) = {
    val loader = Thread.currentThread.getContextClassLoader
    Option(loader.getResource(resourceSourceFolder)).foreach(url => {
      val path = url.getPath
      val targetFolder = new File(fsTargetFolder)
      if(cleanupTarget && targetFolder.exists()) {
        cleanupDirectory(fsTargetFolder)
      }
      FileUtils.copyDirectory(new File(path), targetFolder, false)
    })
  }
  def findInClasspathFolder(fileFolder: String): Iterator[File] = {
    if (isClasspathFileUri(fileFolder)) {
      val resourceFilePath = fileFolder.substring(10)
      val loader = Thread.currentThread.getContextClassLoader
      val url = loader.getResource(resourceFilePath)
      if (url != null) {
        val path = url.getPath
        new File(path).listFiles.toIterator
      }
      else {
        Iterator.empty
      }
    }
    else {
      Iterator.empty
    }
  }

  def getClasspathFileLines(filePath: String): Iterable[String] = {
    if (isClasspathFileUri(filePath)) {
      val file = getClasspathFile(filePath)
      import collection.JavaConverters._
      Files.readAllLines(file.toPath).asScala
    }
    else {
      Iterable.empty
    }
  }

  def encodeToBase64String(src: String) = {
    new String(java.util.Base64.getEncoder.encode(src.getBytes()))
  }

}
