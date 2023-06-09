package datax.test.host

import datax.host.BlobBatchingHost
import datax.host.BlobBatchingHost.inTimeRange
import org.apache.hadoop.fs.{FileStatus, Path}
import org.scalatest.{FlatSpec, Matchers, PrivateMethodTester}

import java.time.Instant
import java.time.temporal.ChronoUnit
import java.util.TimeZone

class BlobBatchingHostTest extends FlatSpec with Matchers with PrivateMethodTester{
  TimeZone.setDefault(TimeZone.getTimeZone("GMT"))
  org.apache.log4j.BasicConfigurator.configure()

  val bbh = BlobBatchingHost

  "getDateTimePattern" should "extract pattern {yyyy/MM/dd}" in {
    val getDateTimePattern = PrivateMethod[String]('getDateTimePattern)
    val result = bbh invokePrivate getDateTimePattern("abfss://container@accountname.dfs.core.windows.net/root/0/{yyyy/MM/dd}/")
    assert(result === "yyyy/MM/dd")
  }

  "getInputBlobPathPrefixes" should "work with pattern {yyyy/MM/dd}" in {
    val StartTime = Instant.parse("2022-08-17T20:00:00Z")
    val EndTime = StartTime.plus(15, ChronoUnit.MINUTES)
    val inputBlobProcessingWindowInSec = StartTime.until(EndTime, ChronoUnit.SECONDS)
    val result = bbh.getInputBlobPathPrefixes(
      path = "abfss://container@accountname.dfs.core.windows.net/root/0/{yyyy/MM/dd}/",
      startTime = StartTime,
      processingWindowInSeconds = StartTime.until(EndTime, ChronoUnit.SECONDS),
      partitionIncrementDurationInSeconds = 60
    )
    assert(result.nonEmpty)
    assert(result.toSeq.head._1 === "abfss://container@accountname.dfs.core.windows.net/root/0/2022/08/17/")
  }

  "getDateTimePattern" should "extract pattern {yyyy/MM/dd/HH}" in {
    val getDateTimePattern = PrivateMethod[String]('getDateTimePattern)
    val result = bbh invokePrivate getDateTimePattern("abfss://container@accountname.dfs.core.windows.net/root/0/{yyyy/MM/dd/HH}/")
    assert(result === "yyyy/MM/dd/HH")
  }

  "getInputBlobPathPrefixes" should "work with pattern {yyyy/MM/dd/HH}" in {
    val StartTime = Instant.parse("2022-08-17T20:00:00Z")
    val EndTime = StartTime.plus(15, ChronoUnit.MINUTES)
    val inputBlobProcessingWindowInSec = StartTime.until(EndTime, ChronoUnit.SECONDS)
    val result = bbh.getInputBlobPathPrefixes(
      path = "abfss://container@accountname.dfs.core.windows.net/root/0/{yyyy/MM/dd/HH}/",
      startTime = StartTime,
      processingWindowInSeconds = StartTime.until(EndTime, ChronoUnit.SECONDS),
      partitionIncrementDurationInSeconds = 60
    )
    assert(result.nonEmpty)
    assert(result.toSeq.head._1 === "abfss://container@accountname.dfs.core.windows.net/root/0/2022/08/17/20/")
  }

  "getDateTimePattern" should "extract pattern {yyyy-MM-dd}" in {
    val getDateTimePattern = PrivateMethod[String]('getDateTimePattern)
    val result = bbh invokePrivate getDateTimePattern("abfss://container@accountname.dfs.core.windows.net/root/0/{yyyy-MM-dd}/")
    assert(result === "yyyy-MM-dd")
  }

  "getInputBlobPathPrefixes" should "work with pattern {yyyy-MM-dd}" in {
    val StartTime = Instant.parse("2022-08-17T20:00:00Z")
    val EndTime = StartTime.plus(15, ChronoUnit.MINUTES)
    val inputBlobProcessingWindowInSec = StartTime.until(EndTime, ChronoUnit.SECONDS)
    val result = bbh.getInputBlobPathPrefixes(
      path = "abfss://container@accountname.dfs.core.windows.net/root/0/{yyyy-MM-dd}/",
      startTime = StartTime,
      processingWindowInSeconds = StartTime.until(EndTime, ChronoUnit.SECONDS),
      partitionIncrementDurationInSeconds = 60
    )
    assert(result.nonEmpty)
    assert(result.toSeq.head._1 === "abfss://container@accountname.dfs.core.windows.net/root/0/2022-08-17/")
  }

  "getDateTimePattern" should "extract pattern {yyyy-MM-dd/HH}" in {
    val getDateTimePattern = PrivateMethod[String]('getDateTimePattern)
    val result = bbh invokePrivate getDateTimePattern("abfss://container@accountname.dfs.core.windows.net/root/0/{yyyy-MM-dd/HH}/")
    assert(result === "yyyy-MM-dd/HH")
  }

  "getInputBlobPathPrefixes" should "work with pattern {yyyy-MM-dd/HH}" in {
    val StartTime = Instant.parse("2022-08-17T20:00:00Z")
    val EndTime = StartTime.plus(15, ChronoUnit.MINUTES)
    val inputBlobProcessingWindowInSec = StartTime.until(EndTime, ChronoUnit.SECONDS)
    val result = bbh.getInputBlobPathPrefixes(
      path = "abfss://container@accountname.dfs.core.windows.net/root/0/{yyyy-MM-dd/HH}/",
      startTime = StartTime,
      processingWindowInSeconds = StartTime.until(EndTime, ChronoUnit.SECONDS),
      partitionIncrementDurationInSeconds = 60
    )
    assert(result.nonEmpty)
    assert(result.toSeq.head._1 === "abfss://container@accountname.dfs.core.windows.net/root/0/2022-08-17/20/")
  }

  "getDateTimePattern" should "extract pattern {'y'=yyyy/'m'=MM/'d'=dd/'h'=HH}" in {
    val getDateTimePattern = PrivateMethod[String]('getDateTimePattern)
    val result = bbh invokePrivate getDateTimePattern("abfss://container@accountname.dfs.core.windows.net/root/3/{'y'=yyyy/'m'=MM/'d'=dd/'h'=HH}/")
    assert(result === "'y'=yyyy/'m'=MM/'d'=dd/'h'=HH")
  }

  "getInputBlobPathPrefixes" should "work with pattern {'y'=yyyy/'m'=MM/'d'=dd/'h'=HH}" in {
    val StartTime = Instant.parse("2022-08-17T20:00:00Z")
    val EndTime = StartTime.plus(15, ChronoUnit.MINUTES)
    val inputBlobProcessingWindowInSec = StartTime.until(EndTime, ChronoUnit.SECONDS)
    val result = bbh.getInputBlobPathPrefixes(
      path = "abfss://container@accountname.dfs.core.windows.net/root/0/{'y'=yyyy/'m'=MM/'d'=dd/'h'=HH}/",
      startTime = StartTime,
      processingWindowInSeconds = StartTime.until(EndTime, ChronoUnit.SECONDS),
      partitionIncrementDurationInSeconds = 60
    )
    assert(result.nonEmpty)
    assert(result.toSeq.head._1 === "abfss://container@accountname.dfs.core.windows.net/root/0/y=2022/m=08/d=17/h=20/")
  }

  val mockFileGen = (time: Long) => {
    val f1 = new FileStatus()
    val field = classOf[FileStatus].getDeclaredField("modification_time")
    field.setAccessible(true)
    field.setLong(f1, time)

    val field2 = classOf[FileStatus].getDeclaredField("path")
    field2.setAccessible(true)
    field2.set(f1, new Path("/", s"${time}"))

    f1
  }

  def setEnv(key: String, value: String) = {
    val field = System.getenv().getClass.getDeclaredField("m")
    field.setAccessible(true)
    val map = field.get(System.getenv()).asInstanceOf[java.util.Map[java.lang.String, java.lang.String]]
    map.put(key, value)
  }

  "inTimeRange" should "process all blobs within hour boundary for 15 min batches (batch start)" in {
    val startTimeStr = "2022-08-17T20:00:00Z"
    val endTimeStr = "2022-08-17T20:14:59Z"
    val StartTime = Instant.parse(startTimeStr)
    val EndTime = Instant.parse(endTimeStr);
    val result = bbh.getInputBlobPathPrefixes(
      path = "abfss://containername@storaccount.dfs.core.windows.net/0/{yyyy/MM/dd/HH}/",
      startTime = StartTime,
      processingWindowInSeconds = StartTime.until(EndTime, ChronoUnit.SECONDS),
      partitionIncrementDurationInSeconds = 1 * 60
    )
    assert(result.nonEmpty)

    val files = List[FileStatus](
      mockFileGen(Instant.parse("2022-08-17T19:59:59Z").toEpochMilli),
      mockFileGen(Instant.parse("2022-08-17T20:00:00Z").toEpochMilli),
      mockFileGen(Instant.parse("2022-08-17T20:00:01Z").toEpochMilli),
      mockFileGen(Instant.parse("2022-08-17T20:14:59Z").toEpochMilli),
      mockFileGen(Instant.parse("2022-08-17T20:15:00Z").toEpochMilli)
    )
    setEnv("process_start_datetime", startTimeStr)
    setEnv("process_end_datetime", endTimeStr)
    val filesToProcess = result.flatMap(_ => files.flatMap(f => inTimeRange(f, filterTimeRange = true)))
      .map(rawName => Instant.ofEpochMilli(rawName.replace("/", "").toLong).toString).toSeq
    assert(filesToProcess.length == 4)

    filesToProcess should contain theSameElementsAs List[String](
      "2022-08-17T19:59:59Z",
      "2022-08-17T20:00:00Z",
      "2022-08-17T20:00:01Z",
      "2022-08-17T20:14:59Z"
    )
  }

  "inTimeRange" should "process all blobs within hour boundary for 15 min batches (batch middle)" in {
    val startTimeStr = "2022-08-17T20:15:00Z"
    val endTimeStr = "2022-08-17T20:29:59Z"
    val StartTime = Instant.parse(startTimeStr)
    val EndTime = Instant.parse(endTimeStr);
    val result = bbh.getInputBlobPathPrefixes(
      path = "abfss:/containername@storaccount.dfs.core.windows.net/0/{yyyy/MM/dd/HH}/",
      startTime = StartTime,
      processingWindowInSeconds = StartTime.until(EndTime, ChronoUnit.SECONDS),
      partitionIncrementDurationInSeconds = 1 * 60
    )
    assert(result.nonEmpty)

    val files = List[FileStatus](
      mockFileGen(Instant.parse("2022-08-17T20:14:59Z").toEpochMilli),
      mockFileGen(Instant.parse("2022-08-17T20:15:00Z").toEpochMilli),
      mockFileGen(Instant.parse("2022-08-17T20:15:01Z").toEpochMilli),
      mockFileGen(Instant.parse("2022-08-17T20:29:59Z").toEpochMilli),
      mockFileGen(Instant.parse("2022-08-17T20:30:00Z").toEpochMilli)
    )
    setEnv("process_start_datetime", startTimeStr)
    setEnv("process_end_datetime", endTimeStr)
    val filesToProcess = result.flatMap(_ => files.flatMap(f => inTimeRange(f, filterTimeRange = true)))
      .map(rawName => Instant.ofEpochMilli(rawName.replace("/", "").toLong).toString).toSeq

    filesToProcess should contain theSameElementsAs List[String](
      "2022-08-17T20:15:00Z",
      "2022-08-17T20:15:01Z",
      "2022-08-17T20:29:59Z"
    )
  }

  "inTimeRange" should "process all blobs within hour boundary for 15 min batches (batch middle 2)" in {
    val startTimeStr = "2022-08-17T20:30:00Z"
    val endTimeStr = "2022-08-17T20:44:59Z"
    val StartTime = Instant.parse(startTimeStr)
    val EndTime = Instant.parse(endTimeStr);
    val result = bbh.getInputBlobPathPrefixes(
      path = "abfss://containername@storaccount.dfs.core.windows.net/0/{yyyy/MM/dd/HH}/",
      startTime = StartTime,
      processingWindowInSeconds = StartTime.until(EndTime, ChronoUnit.SECONDS),
      partitionIncrementDurationInSeconds = 1 * 60
    )
    assert(result.nonEmpty)

    val files = List[FileStatus](
      mockFileGen(Instant.parse("2022-08-17T20:29:59Z").toEpochMilli),
      mockFileGen(Instant.parse("2022-08-17T20:30:00Z").toEpochMilli),
      mockFileGen(Instant.parse("2022-08-17T20:30:01Z").toEpochMilli),
      mockFileGen(Instant.parse("2022-08-17T20:44:59Z").toEpochMilli),
      mockFileGen(Instant.parse("2022-08-17T20:45:00Z").toEpochMilli)
    )
    setEnv("process_start_datetime", startTimeStr)
    setEnv("process_end_datetime", endTimeStr)
    val filesToProcess = result.flatMap(_ => files.flatMap(f => inTimeRange(f, filterTimeRange = true)))
      .map(rawName => Instant.ofEpochMilli(rawName.replace("/", "").toLong).toString).toSeq

    filesToProcess should contain theSameElementsAs List[String](
      "2022-08-17T20:30:00Z",
      "2022-08-17T20:30:01Z",
      "2022-08-17T20:44:59Z"
    )
  }

  "inTimeRange" should "process all blobs within hour boundary for 15 min batches (end)" in {
    val startTimeStr = "2022-08-17T20:45:00Z"
    val endTimeStr = "2022-08-17T20:59:59Z"
    val StartTime = Instant.parse(startTimeStr)
    val EndTime = Instant.parse(endTimeStr);
    val result = bbh.getInputBlobPathPrefixes(
      path = "abfss://containername@storaccount.dfs.core.windows.net/0/{yyyy/MM/dd/HH}/",
      startTime = StartTime,
      processingWindowInSeconds = StartTime.until(EndTime, ChronoUnit.SECONDS),
      partitionIncrementDurationInSeconds = 1 * 60
    )
    assert(result.nonEmpty)

    val files = List[FileStatus](
      mockFileGen(Instant.parse("2022-08-17T20:44:59Z").toEpochMilli),
      mockFileGen(Instant.parse("2022-08-17T20:45:00Z").toEpochMilli),
      mockFileGen(Instant.parse("2022-08-17T20:45:01Z").toEpochMilli),
      mockFileGen(Instant.parse("2022-08-17T20:59:59Z").toEpochMilli),
      mockFileGen(Instant.parse("2022-08-17T21:00:00Z").toEpochMilli)
    )
    setEnv("process_start_datetime", startTimeStr)
    setEnv("process_end_datetime", endTimeStr)
    val filesToProcess = result.flatMap(_ => files.flatMap(f => inTimeRange(f, filterTimeRange = true)))
      .map(rawName => Instant.ofEpochMilli(rawName.replace("/", "").toLong).toString).toSeq

    filesToProcess should contain theSameElementsAs List[String](
      "2022-08-17T20:45:00Z",
      "2022-08-17T20:45:01Z",
      "2022-08-17T20:59:59Z",
      "2022-08-17T21:00:00Z"
    )
  }

}
