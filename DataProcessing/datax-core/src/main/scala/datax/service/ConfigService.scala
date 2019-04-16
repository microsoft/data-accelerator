// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.service

import datax.config.{SettingDictionary}

trait ConfigService {
  def getActiveDictionary(): SettingDictionary
  def setActiveDictionary(conf: SettingDictionary)
}
