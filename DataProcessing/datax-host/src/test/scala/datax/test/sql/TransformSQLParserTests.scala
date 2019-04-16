// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
package datax.test.sql

import datax.sql.{ParsedResult, SqlCommand, TransformSQLParser}
import org.scalatest.{FlatSpec, Matchers}

class TransformSQLParserTests extends FlatSpec with Matchers{
  "DataX-SQL parser" should "work as expected" in {
    val sql = "--DataXQuery--\niottestbatch5s = \nSELECT MIN(myTime) AS __receivedtime,\n      '00000000-0000-0000-0000-000000000000' AS __ruleid,\n\tIoTDeviceId AS __deviceid,\n        MAP('avg', AVG(temperature), 'max', MAX(temperature), 'min', MIN(temperature), 'count', COUNT(temperature)) AS temperature\nFROM DataXProcessedInput\nGROUP BY IoTDeviceId\n--DataXQuery--\niottestbatch5salert = \nSELECT 1 AS `doc.schemaversion`,\n\t'alarm' AS `doc.schema`,\n\t'open' AS status,\n\t'1Rule-1Device-NMessage' AS logic,\n\tunix_timestamp()*1000 AS created,\n\tunix_timestamp()*1000 AS modified,\n\t'Temperature > 80 degrees' AS `rule.description`,\n\t'Critical' AS `rule.severity`,\n\t__ruleid AS `rule.id`,\n\t__deviceid AS `device.id`,\n\tSTRUCT(__ruleid, __deviceid, temperature) AS __aggregates,\n   \t__receivedtime AS `device.msg.received`\nFROM iottestbatch5s\nWHERE temperature.avg>0"

    TransformSQLParser.parse(sql.split("\n")).shouldEqual(ParsedResult(Seq(
      SqlCommand(name="iottestbatch5s", text="SELECT MIN(myTime) AS __receivedtime, '00000000-0000-0000-0000-000000000000' AS __ruleid, IoTDeviceId AS __deviceid, MAP('avg', AVG(temperature), 'max', MAX(temperature), 'min', MIN(temperature), 'count', COUNT(temperature)) AS temperature FROM DataXProcessedInput GROUP BY IoTDeviceId", commandType = TransformSQLParser.CommandType_Query),
      SqlCommand(name="iottestbatch5salert", text="SELECT 1 AS `doc.schemaversion`, 'alarm' AS `doc.schema`, 'open' AS status, '1Rule-1Device-NMessage' AS logic, unix_timestamp()*1000 AS created, unix_timestamp()*1000 AS modified, 'Temperature > 80 degrees' AS `rule.description`, 'Critical' AS `rule.severity`, __ruleid AS `rule.id`, __deviceid AS `device.id`, STRUCT(__ruleid, __deviceid, temperature) AS __aggregates, __receivedtime AS `device.msg.received` FROM iottestbatch5s WHERE temperature.avg>0", commandType = TransformSQLParser.CommandType_Query)
    ), Map[String, Int](
      "iottestbatch5s" -> 1,
      "iottestbatch5salert"->0
    )))
  }
}
