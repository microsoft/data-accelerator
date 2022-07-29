// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.sql

import datax.constants.ProductConstant
import datax.exception.EngineException

import scala.collection.mutable
import scala.collection.mutable.ListBuffer


/// commandType is either 'Query' or 'Command'
case class SqlCommand (text: String, name: String, commandType: String)
case class ParsedResult (commands: Seq[SqlCommand], viewReferenceCount: Map[String, Int])

object TransformSQLParser {
  val CommandType_Query = "Query"
  val CommandType_Command = "Command"

  private def combineLines(lines: Iterable[String]) = lines.filterNot(_.isEmpty).mkString(" ")
  private def buildSqlCommand(text: String, name: String) ={
    SqlCommand(text, name, if(name==null)CommandType_Command else CommandType_Query)
  }

  def parse(lines: Seq[String]):ParsedResult = {
    val commands = new ListBuffer[SqlCommand]
    val viewReferenceCounts = new mutable.HashMap[String, Int]
    val statementBuffer = new ListBuffer[String]

    def appendTable(name: String) = {
      val sql = combineLines(statementBuffer)
      commands.append(buildSqlCommand(sql, name))

      if(name!=null && !name.isEmpty) {
        if (viewReferenceCounts.contains(name)) throw new EngineException(s"dataset name '$name' has been created, please check the query to make sure it is not created again")

        viewReferenceCounts(name) = 0

        viewReferenceCounts.keys.foreach(k => {
          if(s"\\b$k\\b".r.findFirstMatchIn(sql).isDefined){
            viewReferenceCounts(k) += 1
          }
        })
      }
    }

    var tableName:String = null
    var lineNumber = 1
    lines.foreach(line => {
      if(line.trim().isEmpty) {
        // skipped
      }
      else if(line.matches(ProductConstant.ProductQuery)){
        if(statementBuffer.size==0){
          // skipped
        }
        else{
          appendTable(tableName)
        }

        tableName = null
        statementBuffer.clear()
      }
      else{
        "^\\s*--".r.findFirstMatchIn(line) match {
          case Some(m) =>
          //skipped
          case None =>
            if(statementBuffer.length==0){
              "^\\s*([a-zA-Z0-9_]+)\\s*=(.*)$".r.findFirstMatchIn(line) match {
                case Some(m) =>
                  tableName = m.group(1)
                  statementBuffer += m.group(2).trim
                case None =>
                  statementBuffer+=line.trim
              }
            }
            else
              statementBuffer+=line.trim
        }
      }

      lineNumber+=1
    })

    if(tableName!=null && statementBuffer.size>0){
      appendTable(tableName)
      tableName = null
      statementBuffer.clear()
    }

    ParsedResult(commands, viewReferenceCounts.toMap)
  }

  def replaceTableNames(statement: String, tableNameMappings: mutable.HashMap[String, String]) = {
    var result = statement
    for(mapping <- tableNameMappings){
      result = result.replaceAll("\\b"+mapping._1+"\\b", s"`${mapping._2}`")
    }

    result
  }
}

