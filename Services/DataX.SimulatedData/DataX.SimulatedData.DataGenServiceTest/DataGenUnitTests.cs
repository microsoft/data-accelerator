// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DataX.SimulatedData.DataGenService;
using DataX.SimulatedData.DataGenService.Model;
using System;
using System.Collections.Generic;

namespace DataX.SimulatedData.DataGenServiceTest
{
    [TestClass]
    public class DataGenUnitTests
    {
        [TestMethod]
        public void GenerateRandomData1()
        {
            string jsonRaw = System.IO.File.ReadAllText("testinput1.json");

            //seed random
            DataGen dg = new DataGen(1345678);

            var dataSchemaFileContent = JsonConvert.DeserializeObject<DataSchema>(jsonRaw);
            List<JObject> dataStreams = new List<JObject>();
            dg.GenerateRandomData(dataStreams, dataSchemaFileContent.dataSchema[0]);
            DateTime now = DateTime.Now;
            dataStreams[0]["sensordetails"]["timestamp"] = now;

            JObject expectedData = JsonConvert.DeserializeObject<JObject>(System.IO.File.ReadAllText("testrandomexpecteddata1.json"));
            expectedData["sensordetails"]["timestamp"] = now;

            Assert.IsTrue(JToken.DeepEquals(expectedData, dataStreams[0]));
        }

        [TestMethod]
        public void GenerateRandomDataArray()
        {
            string jsonRaw = System.IO.File.ReadAllText("testinput2Array.json");

            //seed random
            DataGen dg = new DataGen(1345678);

            var dataSchemaFileContent = JsonConvert.DeserializeObject<DataSchema>(jsonRaw);
            List<JObject> dataStreams = new List<JObject>();
            dg.GenerateRandomData(dataStreams, dataSchemaFileContent.dataSchema[0]);
            DateTime now = DateTime.Now;
            dataStreams[0]["sensordetails"]["timestamp"] = now;

            JObject expectedData = JsonConvert.DeserializeObject<JObject>(System.IO.File.ReadAllText("testrandomexpecteddata2.json"));
            expectedData["sensordetails"]["timestamp"] = now;

            Assert.IsTrue(JToken.DeepEquals(expectedData, dataStreams[0]));
        }

        [TestMethod]
        public void GenerateRandomRules1()
        {
            string jsonRaw = System.IO.File.ReadAllText("testinput1.json");

            //seed random
            DataGen dg = new DataGen(1345678);

            var dataSchemaFileContent = JsonConvert.DeserializeObject<DataSchema>(jsonRaw);
            List<JObject> dataStreams = new List<JObject>();
            dg.GenerateRandomData(dataStreams, dataSchemaFileContent.dataSchema[0]);
            dg.GenerateDataRules(dataStreams, dataSchemaFileContent.dataSchema[0], 1);
            DateTime now = DateTime.Now;
            dataStreams[0]["sensordetails"]["timestamp"] = now;

            JObject expectedData = JsonConvert.DeserializeObject<JObject>(System.IO.File.ReadAllText("testrulesexpecteddata1.json"));
            expectedData["sensordetails"]["timestamp"] = now;

            //Assert.AreEqual(expectedData, dataStreams[0]);

            Assert.IsTrue(JToken.DeepEquals(expectedData, dataStreams[0]));
        }
    }
}
