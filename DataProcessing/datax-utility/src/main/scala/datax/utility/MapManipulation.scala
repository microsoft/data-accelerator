// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.utility

object MapManipulation {
  def lowercaseKeys[T](map: Map[String, T]): Map[String, T] = {
    if(map==null)
      null
    else
      map.map {
        case (key, value) => key.toLowerCase -> value
      }
  }

  def mergeMapOfStrings(map1: Map[String, String], map2: Map[String, String]): Map[String, String] = {
    if(map1==null)
      map2
    else if(map2==null)
      map1
    else
      map1++map2
  }

  def mergeMapOfDoubles(map1: Map[String, Double], map2: Map[String, Double]): Map[String, Double] = {
    if(map1==null)
      map2
    else if(map2==null)
      map1
    else
      map1++map2
  }

  def addProperty(properties: Map[String, String], prop: String, propValue: String): Map[String, String] = {
    if(propValue==null)
      properties
    else if(properties==null)
      Map(prop->propValue)
    else
      properties + (prop->propValue)
  }
}
