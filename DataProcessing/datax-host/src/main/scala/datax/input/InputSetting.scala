// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.input

import datax.config.{SettingDictionary}

// Interfaces for input config that all input handlers need to implement
trait InputConf {
  val connectionString: String
  val flushExistingCheckpoints: Option[Boolean]
  val repartition: Option[Int]
}

trait InputSetting[T<:InputConf] {
  def getInputConf(dict: SettingDictionary): T
}

