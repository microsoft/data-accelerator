// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.exception

case class EngineException(private val message: String = "", private val cause: Throwable = None.orNull)
  extends Exception(message, cause)
