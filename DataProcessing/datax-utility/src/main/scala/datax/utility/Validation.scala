// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.utility

import datax.exception.EngineException

object Validation {
  def ensureNotNull(param: Any, name : String): Unit ={
    if(param==null)throw new EngineException(s"$param cannot be null")
  }

  def ensureNotEmpty(param: Seq[Any], name: String): Unit = {
    if(param==null)throw new EngineException(s"$param cannot be null")
    if(param.isEmpty)throw new EngineException(s"$param cannot be empty")
  }
}
