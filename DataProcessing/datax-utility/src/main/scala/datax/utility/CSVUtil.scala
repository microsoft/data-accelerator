// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.utility

import datax.service.TelemetryService
import org.apache.log4j.LogManager
import org.apache.spark.sql.SparkSession

object CSVUtil {
  val logger = LogManager.getLogger(this.getClass)

  //Function to load csv type of reference data. In the future we will support other types too. For example: sql etc.
  def loadCSVReferenceData(spark:SparkSession,
                           format: String,
                           path: String,
                           name: String,
                           delimiter: Option[String],
                           header: Option[String],
                           telemetryService: TelemetryService
                          ) = {
    // Load reference data into memory
    //The assumption is that the reference data has a header by default. Also, by default a csv is comma separated
    logger.info(s" ##### referenceDataType: $format ReferenceDataName: $name  ReferenceDataFileName: $path")
    try {
      spark.read.option("delimiter", delimiter.getOrElse(",")).option("header", header.getOrElse("true"))
        .csv(path)
        .createOrReplaceTempView(name)
    }
    catch {
      case e: Exception => {
        telemetryService.trackException(new Exception(s"Cannot find the dataLookupFile from path $path"), Map(
          "errorLocation" -> "loadCSVReferenceData",
          "errorMessage" -> "Failed to extract blob time",
          "failedBlobPath" -> path
        ), null)
        throw new Error(s"The dataLookupFile: ${path} as specified in the configuration does not exist.")
      }
    }
  }
}
