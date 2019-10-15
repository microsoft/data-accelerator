// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.input

import java.sql.Timestamp
import java.text.SimpleDateFormat

import com.fasterxml.jackson.annotation.{JsonCreator, JsonProperty}
import com.fasterxml.jackson.databind.ObjectMapper
import com.microsoft.azure.eventhubs.EventData
import datax.config._
import datax.constants.BlobProperties
import datax.data.FileInternal
import datax.exception.EngineException
import datax.input.BlobPointerInputSetting.BlobPointerInputConf
import datax.sink.BlobOutputSetting.BlobOutputConf
import datax.sink.{BlobOutputSetting, BlobSinker}
import datax.telemetry.AppInsightLogger
import org.apache.log4j.LogManager
import org.apache.spark.rdd.RDD
import org.apache.spark.sql.types.{StringType, StructType}

import scala.collection.mutable
import scala.util.matching.Regex

case class BlobPointer @JsonCreator()(@JsonProperty("BlobPath") BlobPath: String)

object BlobPointerInput {
  val logger = LogManager.getLogger(this.getClass)

  val blobPointerMapper = new ObjectMapper()
  blobPointerMapper.registerSubtypes(classOf[BlobPointer])
  def parseBlobPath(eventData: EventData) =
    blobPointerMapper.readValue(eventData.getBytes, classOf[BlobPointer]).BlobPath

  private def loadBlobPointerSchema() = {
    (new StructType).add("BlobPath", StringType)
  }

  private val saRegex = s"""wasbs?://[\w-]+@([\w\d]+)\${BlobProperties.BlobHostPath}/.*""".r
  private def extractSourceId(blobPath: String, regex: String): String = {
    val r = if(regex == null) saRegex else regex.r
    r.findFirstMatchIn(blobPath) match {
      case Some(partition) => partition.group(1)
      case None => null
    }
  }

  private def extractTimeFromBlobPath(blobPath: String, fileTimeRegex: Regex, fileTimeFormat: String): Timestamp = {
    fileTimeRegex.findFirstMatchIn(blobPath) match {
      case Some(timeStr) => try{
        if(fileTimeFormat==null){
          Timestamp.valueOf(timeStr.group(1).replace('_', ':').replace('T', ' '))
        }
        else{
          val format = new SimpleDateFormat(fileTimeFormat)
          new Timestamp(format.parse(timeStr.group(1)).getTime())
        }
      }
      catch {
        case e: Exception =>
          logger.error(s"Error when parsing date time string from path $blobPath: $e")
          AppInsightLogger.trackException(e, Map(
            "errorLocation" -> "extractTimeFromBlobPath",
            "errorMessage" -> "Error in parsing date time string",
            "failedBlobPath" -> blobPath
          ), null)
          null
      }
      case None =>
        logger.error(s"Failed to extract blob time from path $blobPath")
        AppInsightLogger.trackException(new EngineException(s"Cannot find blob time from path $blobPath"), Map(
          "errorLocation" -> "extractTimeFromBlobPath",
          "errorMessage" -> "Failed to extract blob time",
          "failedBlobPath" -> blobPath
        ), null)
        null
    }
  }

  private  def pathHintsFromBlobPath(blobPath: String, blobPathRegex: Regex): String = {
    blobPathRegex.findFirstMatchIn(blobPath) match {
      case Some(m) => try{
        m.subgroups.mkString("-")
      }
      catch {
        case e: Exception =>
          val msg = s"Error occurs in generating output from blob path. \n Please check: \nregex='$blobPathRegex'\nblobPath='$blobPath'\nmatch='$m'"
          logger.error(msg, e)
          AppInsightLogger.trackException(e, Map(
            "errorLocation" -> "pathHintsFromBlobPath",
            "errorMessage" -> "Error occurs in generating output file name from blob path",
            "failedBlobPath" -> blobPath,
            "regex" -> blobPathRegex.toString()
          ), null)
          //null
          throw new EngineException(msg, e)
      }
      case None =>
        val msg = s"Error occurs in extract file name from blob path. \n Please check: \nregex='$blobPathRegex'\nblobPath='$blobPath'"
        logger.error(msg)
        AppInsightLogger.trackException(new EngineException("Cannot find file name from blob path"), Map(
          "errorLocation" -> "pathHintsFromBlobPath",
          "errorMessage" -> "Error occurs in extracting file name from blob path",
          "failedBlobPath" -> blobPath,
          "regex" -> blobPathRegex.toString()
        ), null)
        //null
        throw new EngineException(msg)
    }
  }

  private def inputPathToInternalProps(inputFilePath: String,
                                   inputConf: BlobPointerInputConf,
                                   outputConf: BlobOutputConf,
                                   outputTimestamp: Timestamp) = {
    val sourceId = extractSourceId(inputFilePath, inputConf.sourceIdRegex)
    inputConf.sources.get(sourceId) match {
      case Some(source) =>
        val fileTime = extractTimeFromBlobPath(inputFilePath, inputConf.fileTimeRegex.r, inputConf.fileTimeFormat)
        val outputPartitionTime = if(outputTimestamp==null) fileTime else outputTimestamp
        FileInternal(inputPath = inputFilePath,
          outputFolders = outputConf.groups.map{case (k,v)=>
            k-> BlobSinker.generateOutputFolderPath(v.folder, outputPartitionTime, Some(source.target))
          },
          outputFileName = pathHintsFromBlobPath(inputFilePath, inputConf.blobPathRegex.r),
          fileTime = fileTime,
          ruleIndexPrefix = source.catalogPrefix.getOrElse(""),
          target = source.target
        )
      case None =>
        FileInternal(inputPath = inputFilePath)
    }
  }

  def pathsToGroups(rdd: RDD[String],
                    jobName: String,
                    dict: SettingDictionary,
                    outputTimestamp: Timestamp) = {
    val initialSet = mutable.HashSet.empty[FileInternal]
    val inputConf = BlobPointerInputSetting.getInputConfig(dict)
    val blobOutputConf = BlobOutputSetting.getDefaultBlobOutputConf(dict)
    rdd.map(s => {
      val propsFile = inputPathToInternalProps(s, inputConf, blobOutputConf, outputTimestamp)
      (if(propsFile.outputFolders==null || propsFile.outputFolders.isEmpty) null else jobName, propsFile)
    })
      .aggregateByKey(initialSet)(_ += _, _ ++ _) // drop duplicates
      .collect()
  }

  def filterPathGroups(groups: Array[(String, mutable.HashSet[FileInternal])]) = {
    groups.find(_._1==null) match {
      case Some(invalidPaths) =>
        logger.warn("Found out-of-scope paths count=" + invalidPaths._2.size + ", First File=" + invalidPaths._2.head.inputPath)
        groups.filter(_._1 != null)
      case None =>
        groups
    }
  }
}
