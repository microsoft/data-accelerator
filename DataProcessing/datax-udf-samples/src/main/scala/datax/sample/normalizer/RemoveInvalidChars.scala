// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.sample.normalizer

import datax.extension.StringNormalizer

/*
  This normalizer replace invalid json characters from the input string with '#' symbol to avoid failing the JSON parser
 */
class RemoveInvalidChars extends StringNormalizer{
  val invalidControlChars = "[\\x00-\\x08\\x0E-\\x1F]".r

  override def normalize(str: String): String = {
    invalidControlChars.replaceAllIn(str, "#")
  }
}
