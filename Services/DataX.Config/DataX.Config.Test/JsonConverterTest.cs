// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DataX.Config.ConfigDataModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DataX.Config.Test
{
    [TestClass]
    public class JsonConverterTest
    {
        private class TestType
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("displayName")]
            public string DisplayName { get; set; }

            [JsonProperty("dynamic")]
            public dynamic Dynamic { get; set; }

            [JsonProperty("generalObject")]
            public JObject GeneralObject { get; set; }

            [JsonProperty("map")]
            public Dictionary<string, dynamic> Map { get; set; }

            [JsonProperty("stringNotExist")]
            public string StringNotExist { get; set; }

            [JsonProperty("longNotExist")]
            public long LongNotExist{ get; set; }
        }

        [TestMethod]
        public void TestDeserializedObject()
        {
            var jsonString = File.ReadAllText(@"Resource\jsonTest.json");
            var instance = JsonConvert.DeserializeObject<TestType>(jsonString);

            Assert.AreEqual(expected: "test", actual: instance.Name);
            Assert.AreEqual(expected: "testDisplayName", actual: instance.DisplayName);
            Assert.AreEqual(expected: "bar", actual: instance.Dynamic.foo.ToString());
            Assert.AreEqual(expected: "bar", actual: instance.GeneralObject.SelectToken("foo").Value<string>());
            Assert.AreEqual(expected: "", actual: instance.Map.GetValueOrDefault("A.B"));
            Assert.AreEqual(expected: 1, actual: instance.Map.GetValueOrDefault("c-d")?.e.Value);
            Assert.IsNull(instance.StringNotExist);
        }

        [TestMethod]
        public void TestDeserializedFlowGuiConfig()
        {
            var jsonString = File.ReadAllText(@"Resource\guiInput.json");
            var json = JsonConfig.From(jsonString);
            var instance = FlowGuiConfig.From(json);

            Assert.AreEqual(expected: "configgentest", actual: instance.Name);
            Assert.AreEqual(expected: "configgentest", actual: instance.DisplayName);
            Assert.AreEqual(expected: "iothub", actual: instance.Input.InputType);
            Assert.AreEqual(expected: "8000", actual: instance.Process.JobConfig.JobExecutorMemory);
        }
    }
}
