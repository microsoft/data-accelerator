// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.extension

/*
  extension to normalize the input string in the streaming pipeline before parsing them as JSON object
 */
trait StringNormalizer {
  def normalize(str: String): String
}
