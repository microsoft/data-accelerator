// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.checkpoint

import datax.fs.HadoopClient
import org.apache.hadoop.conf.Configuration
import org.apache.hadoop.fs.{FileUtil, Path}
import org.apache.log4j.LogManager
import org.apache.spark.eventhubs.{EventHubsConf, EventPosition, NameAndPartition}

object EventhubCheckpointer {
  def getCheckpointFile(checkpointDir: String) = checkpointDir+"/offsets.txt"

  val OffsetTokenSeparator = ","
  def readOffsetsFromFile(file: String): Iterable[(Long, String, Int, Long, Long)] =
    HadoopClient.readHdfsFile(file).filterNot(s=>s==null || s.isEmpty).map(s=>{
      val offset = s.split(OffsetTokenSeparator)
      Tuple5(offset(0).toLong, offset(1), offset(2).toInt, offset(3).toLong, offset(4).toLong)
    })

  def readOffsetsFromCheckpoints(checkpointDir: String): Iterable[(Long, String, Int, Long, Long)] = {
    val conf = HadoopClient.getConf()
    val checkpointFile = getCheckpointFile(checkpointDir)
    val path = new Path(checkpointFile)
    val fs = path.getFileSystem(conf)
    if(fs.exists(path))
      readOffsetsFromFile(checkpointFile)
    else{
      val backupPath = path.suffix(".old")
      if(fs.exists(backupPath)){
        val logger = LogManager.getLogger("readOffsetsFromCheckpoints")
        logger.warn(s"offsets file at checkpoint folder is not found, but found a backup one: ${backupPath.toUri}")
        FileUtil.copy(fs, backupPath, fs, path, false, conf)
        readOffsetsFromFile(checkpointFile)
      }
      else
        null
    }
  }

  def writeOffsetsToCheckpoints(checkpointDir: String, offsets: Seq[(Long, String, Int, Long, Long)], conf: Configuration) = {
    val folder = new Path(checkpointDir)
    val fs = folder.getFileSystem(conf)
    if(!fs.exists(folder)){
      fs.mkdirs(folder)
    }

    val checkpointFile = getCheckpointFile(checkpointDir)
    val path = new Path(checkpointFile)

    if(fs.exists(path)){
      // backup the old one
      val backupPath = path.suffix(".old")
      FileUtil.copy(fs, path, fs, backupPath, false, true, conf)
    }

    HadoopClient.writeHdfsFile(checkpointFile,
      offsets.map(v=>v._1+OffsetTokenSeparator+v._2+OffsetTokenSeparator+v._3+OffsetTokenSeparator+v._4+OffsetTokenSeparator+v._5).mkString("\n"), true)
  }

  def applyCheckpointsIfExists(ehConf: EventHubsConf, checkpointDir: String) = {
    val fromOffsets = readOffsetsFromCheckpoints(checkpointDir)
    val logger = LogManager.getLogger("EventHubConfBuilder")
    if(fromOffsets!=null) {
      logger.warn(s"Checkpoints of offsets are detected. Applying the offsets:\n" + fromOffsets.mkString("\n"))
      ehConf.setStartingPositions(fromOffsets.map { v => new NameAndPartition(v._2, v._3) -> EventPosition.fromSequenceNumber(v._5) }.toMap)
    }
    else{
      logger.warn(s"Checkpoints don't exist, skipped.")
    }
  }
}
