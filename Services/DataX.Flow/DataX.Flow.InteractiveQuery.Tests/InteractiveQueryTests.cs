// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Flow.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace DataX.Flow.InteractiveQuery.Tests
{
    [TestClass]
    public class InteractiveQueryTests
    {
        [TestMethod]
        public void ConvertWasbsToDbfsFilePath()
        {
            //Test ConvertToDbfsFilePath with both filePath and fileName parameters
            string wasbsPath = "wasbs://mycontainer@mystorageaccount.blob.core.windows.net/";
            string fileName = "testFile.json";
            string actualValue = Helper.ConvertToDbfsFilePath(wasbsPath, fileName);
            string expectedValue = "dbfs:/mnt/livequery/mycontainer/testFile.json";
            Assert.AreEqual(expectedValue, actualValue, "DBFS file path is incorrect");

            //Test ConvertToDbfsFilePath with only filePath parameter
            wasbsPath = "wasbs://mycontainer@mystorageaccount.blob.core.windows.net/testfolder/testFile.json";
            actualValue = Helper.ConvertToDbfsFilePath(wasbsPath);
            expectedValue = "dbfs:/mnt/livequery/mycontainer/testfolder/testFile.json";
            Assert.AreEqual(expectedValue, actualValue, "DBFS file path is incorrect");
        }

        [TestMethod]
        public void TestMountCode()
        {
            string expectedValue = "dbutils.fs.mount(source = \"wasbs://mycontainer@teststorageaccount.blob.core.windows.net/\", mountPoint = \"/mnt/livequery//mycontainer\", extraConfigs = Map(\"fs.azure.account.key.teststorageaccount.blob.core.windows.net\"->dbutils.secrets.get(scope = \"testkeyvault\", key = \"datax-sa-teststorageaccount\")))";

            //Test unnested file path
            string dbfsPath = "dbfs:/mnt/livequery/mycontainer/testFile.json";
            string opsStorageAccountName = "teststorageaccount";
            string sparkKeyVaultName = "testkeyvault";
            string actualValue = KernelService.CreateMountCode(dbfsPath, opsStorageAccountName, sparkKeyVaultName);
            Assert.AreEqual(expectedValue, actualValue, "Mount code is incorrect");

            //Test nested file path
            dbfsPath = "dbfs:/mnt/livequery/mycontainer/folder1/folder2/testFile.json";
            actualValue = KernelService.CreateMountCode(dbfsPath, opsStorageAccountName, sparkKeyVaultName);
            Assert.AreEqual(expectedValue, actualValue, "Mount code is incorrect");
        }

        [TestMethod]
        public void TestCreateLoadFunctionCode()
        {
            string realPath = "wasbs://mycontainer@teststorage.blob.core.windows.net/sample/udfsample.jar";
            string functionId = "myFunction";

            // Test CreateLoadFunctionCode for UDF on HDInsight
            string sparkType = "hdinsight";
            string functionType = "UDF";
            string actualValue = KernelService.CreateLoadFunctionCode(
                realPath,
                sparkType,
                functionType,
                functionId,
                new Common.Models.PropertiesUD { ClassName = "SampleClass", Libs = new System.Collections.Generic.List<string>(), Path = "keyvault://testkeyvault/sample" });
            string expectedValue = "val jarPath = \"wasbs://mycontainer@teststorage.blob.core.windows.net/sample/udfsample.jar\"\nval mainClass = \"SampleClass\"\nval jarFileUrl = datax.host.SparkJarLoader.addJarOnDriver(spark, jarPath, 0, false)\nspark.sparkContext.addJar(jarFileUrl)\ndatax.host.SparkJarLoader.registerJavaUDF(spark.udf, \"myFunction\", mainClass, null)\nprintln(\"done\")";
            Assert.AreEqual(expectedValue, actualValue, "Load UDF function code for HDInsight is incorrect");

            // Test CreateLoadFunctionCode for UDF on Databricks
            sparkType = "databricks";
            functionType = "UDF";
            actualValue = KernelService.CreateLoadFunctionCode(
                realPath,
                sparkType,
                functionType,
                functionId,
                new Common.Models.PropertiesUD { ClassName = "SampleClass", Libs = new System.Collections.Generic.List<string>(), Path = "keyvault://testkeyvault/sample" });
            expectedValue = "val jarPath = \"wasbs://mycontainer@teststorage.blob.core.windows.net/sample/udfsample.jar\"\nval mainClass = \"SampleClass\"\nval jarFileUrl = datax.host.SparkJarLoader.addJarOnDriver(spark, jarPath, 0, true)\nspark.sparkContext.addJar(jarFileUrl)\ndatax.host.SparkJarLoader.registerJavaUDF(spark.udf, \"myFunction\", mainClass, null)\nprintln(\"done\")";
            Assert.AreEqual(expectedValue, actualValue, "Load UDF function code for Databricks is incorrect");

            // Test CreateLoadFunctionCode for UDAF on HDInsight
            sparkType = "hdinsight";
            functionType = "UDAF";
            actualValue = KernelService.CreateLoadFunctionCode(
                realPath,
                sparkType,
                functionType,
                functionId,
                new Common.Models.PropertiesUD { ClassName = "SampleClass", Libs = new System.Collections.Generic.List<string>(), Path = "keyvault://testkeyvault/sample" });
            expectedValue = "val jarPath = \"wasbs://mycontainer@teststorage.blob.core.windows.net/sample/udfsample.jar\"\nval mainClass = \"SampleClass\"\nval jarFileUrl = datax.host.SparkJarLoader.addJarOnDriver(spark, jarPath, 0, false)\nspark.sparkContext.addJar(jarFileUrl)\ndatax.host.SparkJarLoader.registerJavaUDAF(spark.udf, \"myFunction\", mainClass)\nprintln(\"done\")";
            Assert.AreEqual(expectedValue, actualValue, "Load UDAF function code for HDInsight is incorrect");

            // Test CreateLoadFunctionCode for UDAF on Databricks
            sparkType = "databricks";
            functionType = "UDAF";
            actualValue = KernelService.CreateLoadFunctionCode(
                realPath,
                sparkType,
                functionType,
                functionId,
                new Common.Models.PropertiesUD { ClassName = "SampleClass", Libs = new System.Collections.Generic.List<string>(), Path = "keyvault://testkeyvault/sample" });
            expectedValue = "val jarPath = \"wasbs://mycontainer@teststorage.blob.core.windows.net/sample/udfsample.jar\"\nval mainClass = \"SampleClass\"\nval jarFileUrl = datax.host.SparkJarLoader.addJarOnDriver(spark, jarPath, 0, true)\nspark.sparkContext.addJar(jarFileUrl)\ndatax.host.SparkJarLoader.registerJavaUDAF(spark.udf, \"myFunction\", mainClass)\nprintln(\"done\")";
            Assert.AreEqual(expectedValue, actualValue, "Load UDAF function code for Databricks is incorrect");
        }

        [TestMethod]
        public void TestTranslateBinNames()
        {
            string json = @"[
		        'path1/path2/a.jar',
		        'path1/path2/b.jar',
		        'path1/path2/c.jar'
            ]";

            JArray binNames = JArray.Parse(json);
            var actual = InteractiveQueryManager.TranslateBinNames(binNames, "myAccount", "myContainer");

            string expected = "wasbs://myContainer@myAccount.blob.core.windows.net/path1/path2/a.jar,wasbs://myContainer@myAccount.blob.core.windows.net/path1/path2/b.jar,wasbs://myContainer@myAccount.blob.core.windows.net/path1/path2/c.jar";

            Assert.AreEqual(expected, actual);
        }


        [TestMethod]
        public void TestConvertToJson()
        {
            string result = @"org.apache.spark.sql.catalyst.parser.ParseException:\r\nmismatched input '<EOF>' expecting { '(', 'SELECT'} (line 1, pos 146)";
            var json = KernelService.ConvertToJson(10, result);
                        
            Assert.IsTrue(json.Error.HasValue && json.Error.Value);
            Assert.IsNotNull(json.Message);

            string source = @"{'prop1':6, 'prop2': 'evnetName1', 'status':0}
{'prop1':6, 'prop2': 'evnetName2', 'status':1}
Input: org.apache.spark.sql.DataFrame = [prop1: bigint, prop2: string... 1 more field]";

            json = KernelService.ConvertToJson(10, source.ToString());

            Assert.IsTrue(!json.Error.HasValue);
            Assert.AreEqual(2, JArray.Parse(json.Result.ToString()).Count);

            source = @"{'prop1':6, 'prop2': 'evnetName1', 'status':0}
{'prop1':6, 'prop2': 'evnetName2', 'status':1}";
            json = KernelService.ConvertToJson(10, source.ToString());

            Assert.IsTrue(!json.Error.HasValue);
            Assert.AreEqual(2, JArray.Parse(json.Result.ToString()).Count);
        }
    }
}
