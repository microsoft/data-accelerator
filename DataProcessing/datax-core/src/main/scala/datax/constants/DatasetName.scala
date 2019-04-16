// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.constants

object DatasetName {
  def DataStreamRaw = s"${NamePrefix.Value}RawInput"
  def DataStreamProjection = s"${NamePrefix.Value}ProcessedInput"
  def DataStreamProjectionBatch = s"${NamePrefix.Value}ProcessedInput_Batch"
  def DataStreamProjectionWithWindow = s"${NamePrefix.Value}ProcessedInput_Window"
}
