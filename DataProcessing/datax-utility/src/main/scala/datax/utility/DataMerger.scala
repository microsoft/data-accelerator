// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.utility

object DataMerger {
  def flattenMapOfCounts(map: Map[String, Map[String, Int]]): Map[String, Int] = {
    map.flatMap(m=>m._2.map(m2=>m._1+"_"+m2._1->m2._2))
  }

  def mergeMapOfCounts(map1: Map[String, Int], map2: Map[String, Int]): Map[String, Int] = {
    map1 ++ map2.map{ case (k,v) => k -> (v + map1.getOrElse(k,0)) }
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
}
