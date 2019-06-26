// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Flow.Common
{
    public static class TemplateGenerator
    {
        public static string GetSetupStepsTemplate()
        {
            const string template = @"<?xml version=""1.0"" encoding=""utf-8""?>
<steps>  
<step seq=""0"">%%configure -f
{ ""conf"": {
""spark.jars"": ""<@BinName>"",
""spark.driver.cores"": 1,
""spark.driver.memory"": ""1024m"",
""spark.executor.memory"": ""1024m"",
""spark.executor.instances"": 2,
""spark.executor.cores"": 1,
""spark.app.name"": ""<@KernelDisplayName>""
}}
</step>
<step seq = ""1"">
import java.text.{SimpleDateFormat, ParseException}
import org.apache.log4j.{Level, LogManager}
import java.util.Date
import java.sql.Timestamp
val timeFormats = Array(
new SimpleDateFormat(""yyyy-MM-dd'T'HH:mm:ss'Z'""),
new SimpleDateFormat(""MM/dd/yyyy HH:mm:ss"")
)
def tryParseStringToDate(format: SimpleDateFormat, str: String): Date = {
try{
format.parse(str)
}
catch{
case _: ParseException => null
}
}

def stringToTimestamp(s: String): Timestamp = {
if(s==null || s.isEmpty)
null	
else{
try{
Timestamp.valueOf(s)
}
catch {
case _: IllegalArgumentException =>
timeFormats.iterator.map(f=>tryParseStringToDate(f, s)).collectFirst{
case d: Date if d!=null => Timestamp.from(d.toInstant)
} match {
case Some(value) => value
case None =>
val logger = LogManager.getLogger(""stringToTimestamp"")
logger.error(s""Failed to parse timestamp from string '$s' with data format [${timeFormats.mkString("","")}]"")
null
}
case e: Exception =>
val logger = LogManager.getLogger(""stringToTimestamp"")
logger.error(s""Failed to parse timestamp from string '$s'"", e)
null
}
}
}
spark.udf.register(""stringToTimestamp"", stringToTimestamp _)
</step>
<step seq = ""2"" >
import org.apache.spark.sql.types._
val rawSchemaString = """"""<@RawSchema>""""""
val rawSchema = DataType.fromJson(rawSchemaString)
</step>
<step seq = ""3"">
import org.apache.spark.sql.functions._
val file = ""<@SampleDataPath>""
val rawData = spark.read.json(file)
rawData.select(from_json(col(""Raw""), rawSchema).alias(""Raw""), col(""Properties""), col(""SystemProperties"")).selectExpr(<@NormalizationSnippet>).createOrReplaceTempView(""DataXProcessedInput"")
datax.host.UdfInitializer.initialize(spark,null)
print(""done"")
</step>
<step seq = ""4"" >
val result = sql(""SELECT * FROM DataXProcessedInput LIMIT 10"")
result.show(false)
</step>
</steps>
";
            return template;
        }
    }
}
