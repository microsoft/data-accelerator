// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.config

import org.apache.spark.SparkConf

case class UnifiedConfig(
                         sparkConf: SparkConf,
                         dict: SettingDictionary
                       )
