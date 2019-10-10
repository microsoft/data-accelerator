// *********************************************************************
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.ServerScenarios;
using DataXScenarios;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using ScenarioTester;
using System;

namespace ScenarioTesterTests
{
    [TestClass]
    public class TestDataXScenarios
    {

        private void SetContextValues(ContextHelper helper, params string[] keyValuePairs)
        {
            // List of pairs needs to be even
            if(keyValuePairs.Length == 0 || keyValuePairs.Length % 2 != 0)
            {
                throw new Exception("Invalid amount of key value pairs");
            }
            for(int i = 0; i < keyValuePairs.Length; i++)
            {
                if(i % 2 != 0)
                {
                    string key = keyValuePairs[i - 1];
                    string value = keyValuePairs[i];
                    helper.SetContextValue<string>(key, value);
                }
            }
        }

        [TestMethod]
        public void SetAndGetContextStringValue()
        {
            var helper = new ContextHelper(new ScenarioContext());
            helper.SetContextValue<string>("DATAX_STRING", "VALUE");
            string value = helper.GetContextValue<string>("DATAX_STRING");
            Assert.IsNotNull(value);
            Assert.IsFalse(string.IsNullOrEmpty(value));
        }

        [TestMethod]
        public void CreateUrlFromPath()
        {
            var helper = new ContextHelper(new ScenarioContext());
            helper.SetContextValue<string>(Context.ServiceUrl, "https://server");
            var url = helper.CreateUrl("/api/flows");
            Assert.AreEqual($"https://server/api/flows", url);

        }

        [TestMethod]
        public void GetInferSchemaJsonIsValid()
        {
            string flowNameValue = "flowName";
            string connectionStringValue = "connectionString";
            string eventHubNameValue = "hubName";
            string secondsValue = "seconds";
            var helper = new ContextHelper(new ScenarioContext());
            SetContextValues(helper,
                Context.FlowName, flowNameValue,
                Context.EventhubConnectionString, connectionStringValue,
                Context.EventHubName, eventHubNameValue,
                Context.Seconds, secondsValue);
            string jsonString = DataXHost.GetInferSchemaJson(helper);
            Assert.IsNotNull(jsonString);
            dynamic json = JObject.Parse(jsonString);
            Assert.IsNotNull(json);
            Assert.AreEqual(flowNameValue, (string)json.name);
            Assert.AreEqual(flowNameValue, (string)json.userName);
            Assert.AreEqual(connectionStringValue, (string)json.eventhubConnectionString);
            Assert.AreEqual(eventHubNameValue, (string)json.eventHubNames);
            Assert.AreEqual(secondsValue, (string)json.seconds);
        }

        [TestMethod]
        public void GetInitializeKernelJsonIsValid()
        {
            string flowNameValue = "flowName";
            string connectionStringValue = "connectionString";
            string eventHubNameValue = "hubName";
            string inputSchemaValue = "inputSchema";
            string kernelIdValue = "kernelId";
            string normalizationSnippetValue = "snippet";
            var helper = new ContextHelper(new ScenarioContext());
            SetContextValues(helper,
                Context.FlowName, flowNameValue,
                Context.EventhubConnectionString, connectionStringValue,
                Context.EventHubName, eventHubNameValue,
                Context.InputSchema, $"\"{inputSchemaValue}\"",
                Context.KernelId, kernelIdValue,
                Context.NormalizationSnippet, $"\"{normalizationSnippetValue}\"");
            string jsonString = DataXHost.GetInitializeKernelJson(helper);
            Assert.IsNotNull(jsonString);
            dynamic json = JObject.Parse(jsonString);
            Assert.AreEqual(flowNameValue, (string)json.name);
            Assert.AreEqual(flowNameValue, (string)json.userName);
            Assert.AreEqual(connectionStringValue, (string)json.eventhubConnectionString);
            Assert.AreEqual(eventHubNameValue, (string)json.eventHubNames);
            Assert.AreEqual(inputSchemaValue, (string)json.inputSchema);
            Assert.AreEqual(kernelIdValue, (string)json.kernelId);
            Assert.AreEqual(normalizationSnippetValue, (string)json.normalizationSnippet);
        }

        [TestMethod]
        public void GetDeleteKernelJsonIsValid()
        {
            string kernelIdValue = "kernelIdValue";
            string flowNameValue = "flowNameValue";
            var helper = new ContextHelper(new ScenarioContext());
            SetContextValues(helper,
                Context.KernelId, kernelIdValue,
                Context.FlowName, flowNameValue);
            string jsonString = DataXHost.GetDeleteKernelJson(helper);
            Assert.IsNotNull(jsonString);
            dynamic json = JObject.Parse(jsonString);
            Assert.AreEqual(flowNameValue, (string)json.name);
            Assert.AreEqual(kernelIdValue, (string)json.kernelId);
        }
    }
}
