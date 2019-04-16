// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.config

import datax.constants.ProductConstant

object SettingNamespace {
  val DefaultSettingName = ""
  val Seperator = "."
  val ValueSeperator = ";"
  def Root = ProductConstant.ProductRoot
  def RootPrefix = Root + Seperator
  val Job = "job"
  def JobPrefix = RootPrefix + Job + Seperator

  val JobName = "name"
  def JobNameFullPath = JobPrefix + JobName

  val JobInput = "input.default"
  def JobInputPrefix = JobPrefix + JobInput + Seperator

  val JobProcess = "process"
  def JobProcessPrefix = JobPrefix + JobProcess + Seperator

  val JobOutput = "output"
  def JobOutputPrefix = JobPrefix + JobOutput + Seperator

  val JobOutputDefault = "default"
  def JobOutputDefaultPreifx = JobOutputPrefix + JobOutputDefault + Seperator

  def buildSettingPath(names: String*) = {
    names.filterNot(_==null).mkString(Seperator)
  }

  def getSubNamespace(propName: String, startIndex: Int): String = {
    if(propName.length>startIndex) {
      val pos = propName.indexOf(SettingNamespace.Seperator, startIndex)
      if(pos>=0)
        propName.substring(startIndex, pos)
      else
        propName.substring(startIndex)
    }
    else
      null
  }
}
