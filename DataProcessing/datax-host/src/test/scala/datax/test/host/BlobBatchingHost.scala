package datax.test.host

import datax.host.BlobBatchingHost
import org.scalatest.flatspec.AnyFlatSpec
import org.scalatest.matchers.should.Matchers
import org.scalatest.PrivateMethodTester

import java.time.Instant
import java.time.temporal.{ChronoUnit, TemporalUnit}
import java.util.TimeZone

class BlobBatchingHost extends AnyFlatSpec with Matchers with PrivateMethodTester{
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

}
