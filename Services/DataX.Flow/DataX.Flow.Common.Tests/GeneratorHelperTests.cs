// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataX.Flow.Common;
using DataX.Config.ConfigDataModel;

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
    }
}
