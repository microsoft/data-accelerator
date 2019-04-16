// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.utility

object DataNormalization {
  def sanitizeColumnName(name: String) = {
    val invalidChars = ".- '"
    if(invalidChars.find(name.contains(_)).isDefined) s"`$name`" else name
  }
}
