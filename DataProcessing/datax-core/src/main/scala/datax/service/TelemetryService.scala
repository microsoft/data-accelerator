// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.service

import java.sql.Timestamp

trait TelemetryService {
  def trackEvent(event: String, properties: Map[String, String], measurements: Map[String, Double])
  def trackException(e: Exception, properties: Map[String, String], measurements: Map[String, Double])
  def trackBatchEvent(event: String, properties: Map[String, String], measurements: Map[String, Double], batchTime: Timestamp)
}
