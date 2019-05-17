// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.input

import datax.config.{SettingDictionary}
import org.apache.spark.sql.types.{DataType}


case class InputLocalConf( inputSchema:DataType,
                           connectionString: String,
                           flushExistingCheckpoints: Option[Boolean],
                           repartition: Option[Int]
                        ) extends InputConf

object LocalInputSetting extends InputSetting[InputLocalConf] {

  def getInputConf(dict: SettingDictionary): InputLocalConf = {
    val inputSchema = SchemaFile.loadInputSchema(dict)
    InputLocalConf(inputSchema,null,null,null)
  }
}