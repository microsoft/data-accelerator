package datax.test.testutils

import com.globalmentor.apache.hadoop.fs.BareLocalFileSystem
import org.apache.hadoop.fs.FileSystem
import org.apache.spark.sql.SparkSession

trait SparkSessionTestWrapper {

  def createTestSparkSession(useLocalFS: Boolean = true) = {
    val spark = SparkSession
      .builder()
      .master("local")
      .appName("spark test")
      .getOrCreate()
    if(useLocalFS) {
      spark.sparkContext.hadoopConfiguration.setClass("fs.file.impl", classOf[BareLocalFileSystem], classOf[FileSystem])
    }
    spark
  }

  def setEnv(key: String, value: String) = {
    val field = System.getenv().getClass.getDeclaredField("m")
    field.setAccessible(true)
    val map = field.get(System.getenv()).asInstanceOf[java.util.Map[java.lang.String, java.lang.String]]
    map.put(key, value)
  }

}
