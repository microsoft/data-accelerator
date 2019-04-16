// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.utility

object GuidUtil {
  def generateGuid(): String = {
    java.util.UUID.randomUUID().toString
  }
}
