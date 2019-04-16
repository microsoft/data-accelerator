// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.data

import java.sql.Timestamp

import datax.utility.DateTimeUtil
import org.apache.spark.SparkEnv
import org.apache.spark.sql.Row
import org.apache.spark.sql.functions.udf

case class FileInternal(inputPath: String = null,
                            outputFolders: scala.collection.Map[String, String] = null,
                            outputFileName: String = null,
                            fileTime: Timestamp = null,
                            ruleIndexPrefix: String = null,
                            target: String = null
                           )

object FileInternal {
  def appendEmptyInternalInfo(json: String) = {
    (FileInternal(), json)
  }

  def udfEmptyInternalInfo = udf(()=>FileInternal())

  val getInfoInputPath = (rowInfo: Row) => rowInfo.getString(0)
  val getInfoOutputFolder = (rowInfo: Row, group: String) => if(rowInfo.isNullAt(1)) null else rowInfo.getMap[String, String](1).getOrElse(group, null)
  val getInfoOutputFileName = (rowInfo: Row) => rowInfo.getString(2)
  val getInfoFileTimeString = (rowInfo: Row) => if(rowInfo.isNullAt(3))null else rowInfo.getTimestamp(3).toString
  val getInfoRuleIndexPrefix = (rowInfo: Row) => rowInfo.getString(4)
  val getInfoTargetTag = (rowInfo: Row) => rowInfo.getString(5)

}
