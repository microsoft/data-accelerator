// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.utility

import java.util.function.Function
import scala.language.implicitConversions

object ConverterUtil {
  implicit def scalaFunctionToJava[From, To](function: (From) => To): Function[From, To] = {
    new Function[From, To] {
      override def apply(input: From): To = function(input)
    }
  }
}
