// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.constants

object ProductConstant {
  def DefaultAppName = s"${NamePrefix.Value}_Unknown_App"
  def MetricAppNamePrefix = s"${NamePrefix.Value}-".toUpperCase
  def ProductRoot = s"${NamePrefix.Value}".toLowerCase
  def ProductJobTags = s"${NamePrefix.Value}JobTags"
  def ProductRedisBase = s"${NamePrefix.Value}_RedisBase"
  def ProductRedisStandardConnection = s"${NamePrefix.Value}_RedisStandardConnection"
  def ProductRedisClusterConnection = s"${NamePrefix.Value}_RedisClusterConnection"
  def DataStreamProcessDataSetLogger = s"${NamePrefix.Value}-ProcessDataset"
  def ProductInstrumentLogger = s"${NamePrefix.Value}-Instrument"
  def ProductOutputFilter = s"${NamePrefix.Value}OutputFilter"
  def ProductQuery = s"^--${NamePrefix.Value}Query--"
}
