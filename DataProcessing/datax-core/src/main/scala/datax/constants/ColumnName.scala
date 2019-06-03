// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.constants


object ColumnName {


  // Define constants for column names
  val RawObjectColumn = "Raw"
  val EventNameColumn = "EventName"
  def PropertiesColumn = s"${NamePrefix.Value}Properties"
  def RawPropertiesColumn = "Properties"
  def RawSystemPropertiesColumn = "SystemProperties"

  def InternalColumnPrefix = s"__${NamePrefix.Value}_"
  def InternalColumnFileInfo = InternalColumnPrefix + "FileInfo"
  def MetadataColumnPrefix = s"__${NamePrefix.Value}Metadata_"
  def MetadataColumnOutputPartitionTime = MetadataColumnPrefix + "OutputPartitionTime"

  def OutputGroupColumn = s"${NamePrefix.Value}OutputGroup"

}
