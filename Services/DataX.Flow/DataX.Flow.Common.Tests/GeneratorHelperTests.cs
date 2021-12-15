// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataX.Flow.Common;
using DataX.Config.ConfigDataModel;
using System.Collections.Generic;

namespace DataX.Flow.Generator.Tests
{
    [TestClass]
    public class FlowGeneratorHelperTests
    {
        [TestMethod]
        public void IsKeyVaultTest()
        {
            Assert.IsTrue(Helper.IsKeyVault("keyvault://abc"));
            Assert.IsTrue(Helper.IsKeyVault("secretscope://abc"));
            Assert.IsFalse(Helper.IsKeyVault("abc//keyvault://abc"));
            Assert.IsFalse(Helper.IsKeyVault("abc.secretscope://abc"));
        }

        [TestMethod]
        public void ParseEventHubTest()
        {
            Assert.AreEqual("testeventhubname", Helper.ParseEventHub(@"Endpoint=sb://testnamespace.servicebus.windows.net/;SharedAccessKeyName=policyname;SharedAccessKey=12345=;EntityPath=testeventhubname"));

            Assert.AreEqual(null, Helper.ParseEventHub(@"Endpoint=sb://testnamespace.servicebus.windows.net/;SharedAccessKeyName=policyname;SharedAccessKey=12345"));
        }

        [TestMethod]
        public void ParseEventHubNamesTest()
        {
            var names = Helper.ParseEventHubNames("test1, test2");
            Assert.AreEqual("test1", names[0]);
            Assert.AreEqual("test2", names[1]);
        }

        [TestMethod]
        public void ParseEventHubNamespaceTest()
        {
            Assert.AreEqual("testnamespace", Helper.ParseEventHubNamespace(@"Endpoint=sb://testnamespace.servicebus.windows.net/;SharedAccessKeyName=policyname;SharedAccessKey=12345=;EntityPath=testeventhubname"));

            Assert.AreEqual("testnamespace", Helper.ParseEventHubNamespace(@"Endpoint=sb://testnamespace.servicebus.windows.net/;SharedAccessKeyName=policyname;SharedAccessKey=12345"));
        }

        [TestMethod]
        public void ParseEventHubPolicyNameTest()
        {
            Assert.AreEqual("policyname", Helper.ParseEventHubPolicyName(@"Endpoint=sb://testnamespace.servicebus.windows.net/;SharedAccessKeyName=policyname;SharedAccessKey=12345=;EntityPath=testeventhubname"));

            Assert.AreEqual("policyname", Helper.ParseEventHubPolicyName(@"Endpoint =sb://testnamespace.servicebus.windows.net/;SharedAccessKeyName=policyname;SharedAccessKey=12345"));
        }

        [TestMethod]
        public void ParseEventHubAccessKeyTest()
        {
            Assert.AreEqual("12345=", Helper.ParseEventHubAccessKey(@"Endpoint=sb://testnamespace.servicebus.windows.net/;SharedAccessKeyName=policyname;SharedAccessKey=12345=;EntityPath=testeventhubname"));

            Assert.AreEqual("12345", Helper.ParseEventHubAccessKey(@"Endpoint =sb://testnamespace.servicebus.windows.net/;SharedAccessKeyName=policyname;SharedAccessKey=12345"));
        }

        [TestMethod]
        public void GetSecretFromKeyvaultIfNeededTest()
        {
            Assert.AreEqual("value", Helper.GetSecretFromKeyvaultIfNeeded(@"value"));
        }

        [TestMethod]
        public void GetKeyVaultNameTest()
        {
            Assert.AreEqual("keyvault://somekeyvalut/test-input-connectionstring-CD42404D52AD55CCFA9ACA4ADC828AA5", Helper.GetKeyVaultName("somekeyvalut", "test-input-connectionstring", Constants.SparkTypeHDInsight, "value"));

            Assert.AreEqual("keyvault://somekeyvalut/test-input-connectionstring", Helper.GetKeyVaultName("somekeyvalut", "test-input-connectionstring", Constants.SparkTypeHDInsight, "value", false));

            Assert.AreEqual("secretscope://somekeyvalut/test-input-connectionstring-CD42404D52AD55CCFA9ACA4ADC828AA5", Helper.GetKeyVaultName("somekeyvalut", "test-input-connectionstring", Constants.SparkTypeDataBricks, "value"));

            Assert.AreEqual("secretscope://somekeyvalut/test-input-connectionstring", Helper.GetKeyVaultName("somekeyvalut", "test-input-connectionstring", Constants.SparkTypeDataBricks, "value", false));
        }

        [TestMethod]
        public void GetBootstrapServersTest()
        {
            Assert.AreEqual("testnamespace.servicebus.windows.net:9093", Helper.TryGetBootstrapServers(@"Endpoint=sb://testnamespace.servicebus.windows.net/;SharedAccessKeyName=policyname;SharedAccessKey=12345=;EntityPath=testeventhubname"));

            Assert.AreEqual("testnamespace.servicebus.windows.net:9093", Helper.TryGetBootstrapServers(@"testnamespace.servicebus.windows.net:9093"));
        }

        [TestMethod]
        public void ParseConnectionStringTest()
        {
            var result1 = Helper.ParseConnectionString(@"endpoint=https://myserver.azurehdinsight.net/livy;username=user12345;password=passwd12345");

            Assert.AreEqual("https://myserver.azurehdinsight.net/livy", result1.Endpoint);
            Assert.AreEqual("passwd12345", result1.Password);
            Assert.AreEqual("user12345", result1.UserName);
        }

        [TestMethod]
        public void TranslateOutputTemplateTest()
        {
            var source = @"<?xml version=""1.0"" encoding=""utf-8""?>
            <steps>  
            <step seq=""0"">%%configure -f
            { ""conf"": {
            ""spark.jars"": ""<@BinName>"",
            ""spark.app.name"": ""<@KernelDisplayName>""
            }}";

            var expected = @"<?xml version=""1.0"" encoding=""utf-8""?>
            <steps>  
            <step seq=""0"">%%configure -f
            { ""conf"": {
            ""spark.jars"": ""test"",
            ""spark.app.name"": ""test2""
            }}";

            Dictionary<string, string> values = new Dictionary<string, string>
            {
                ["BinName"] = "test",
                ["KernelDisplayName"] = "test2"
            };
            var actual = Helper.TranslateOutputTemplate(source, values);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ParseCosmosDBEndPointTest()
        {
            Assert.AreEqual("dbname.documents.azure.com", Helper.ParseCosmosDBEndPoint(@"mongodb://dbname:key==@dbname.documents.azure.com:10255/?ssl=true&replicaSet=globaldb"));

            Assert.AreEqual("", Helper.ParseCosmosDBEndPoint(@"connection string"));
        }

        [TestMethod]
        public void ParseCosmosDBUserNamePasswordTest()
        {
            Assert.AreEqual("dbname:key==", Helper.ParseCosmosDBUserNamePassword(@"mongodb://dbname:key==@dbname.documents.azure.com:10255/?ssl=true&replicaSet=globaldb"));

            Assert.AreEqual("", Helper.ParseCosmosDBUserNamePassword(@"connection string"));
        }

        [TestMethod]
        public void GenerateNewSecretTest()
        {
            var dict = new Dictionary<string, string>();

            Assert.AreEqual("keyvault://somekeyvalut/test-input-connectionstring-3C9683017F9E4BF33D0FBEDD26BF143F", Helper.GenerateNewSecret(dict, "somekeyvalut", "test-input-connectionstring", Constants.SparkTypeHDInsight, "value1"));
            Assert.AreEqual(1, dict.Count);
            Assert.AreEqual("value1", dict["keyvault://somekeyvalut/test-input-connectionstring-3C9683017F9E4BF33D0FBEDD26BF143F"]);

            Assert.AreEqual("secretscope://somekeyvalut/test-input-connectionstring", Helper.GenerateNewSecret(dict, "somekeyvalut", "test-input-connectionstring", Constants.SparkTypeDataBricks, "value2", false));

            Assert.AreEqual(2, dict.Count);
            Assert.AreEqual("value2", dict["secretscope://somekeyvalut/test-input-connectionstring"]);
        }
    }
}
