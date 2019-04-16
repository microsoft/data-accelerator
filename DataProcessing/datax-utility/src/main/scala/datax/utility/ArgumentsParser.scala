// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.utility

object ArgumentsParser {
  def getNamedArgs(args: Array[String]): Map[String, String] = {
    args.filter(line => line.contains("=")) //take only named arguments
      .map(x => (x.substring(0, x.indexOf("=")), x.substring(x.indexOf("=") + 1))) //split into key values
      .toMap //convert to a map
  }
}
