// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace DataX.Flow.SchemaInference.Tests
{
    [TestClass]
    public class SchemaTests
    {
        [TestMethod]
        public void SchemaTypesTest()
        {
            string events = GetEvents("events.json");
            var schema = GetSchema(events);
            List<string> expectedResult = GetExpectedSchema();

            Assert.AreEqual(expectedResult[0].Trim(), schema.Schema, "Schema generation is incorrect");
        }

        [TestMethod]
        public void SchemaTopLevelArrayTest()
        {
            string events = GetEvents("events4.json");
            var schema = GetSchema(events);
            List<string> expectedResult = GetExpectedSchema();

            Assert.AreEqual(expectedResult[1].Trim(), schema.Schema, "Schema generation is incorrect");
        }

        [TestMethod]
        public void SchemaConflictTest()
        {
            string events = GetEvents("events5.json");
            var schema = GetSchema(events);
            List<string> expectedResult = GetExpectedSchema();

            Assert.AreEqual(expectedResult[2].Trim(), schema.Schema, "Schema generation is incorrect");

            string expectedErrors = GetErrors("events5errors.txt");
            string actualErrors = GetErrorString(schema.Errors);
            Assert.AreEqual(expectedErrors.Trim(), actualErrors.Trim(), "Errors are incorrect");
        }

        private string GetEvents(string file)
        {
            StreamReader sr = new StreamReader(file);
            string events = sr.ReadToEnd();
            sr.Close();
            return events;
        }

        private SchemaResult GetSchema(string events)
        {
            Engine engine = new Engine();
            var result = engine.GetSchema(events);
            return result;
        }

        private List<string> GetExpectedSchema()
        {
            StreamReader sr = new StreamReader("result.txt");
            string result = sr.ReadToEnd();
            sr.Close();

            List<string> retVal = new List<string>(result.Split('\n'));
            return retVal;
        }

        //private List<string> GetErrors(string file)
        private string GetErrors(string file)
        {
            StreamReader sr = new StreamReader(file);
            string result = sr.ReadToEnd();
            sr.Close();

            //List<string> retVal = new List<string>(result.Split('\n'));
            //return retVal;
            return result;
        }

        private string GetErrorString(List<string> errors)
        {
            string result = string.Empty;
            if (errors == null || errors.Count <= 0)
            {
                return result;
            }

            foreach (string s in errors)
            {
                result += s + "\r\n";
            }

            return result;
        }
    }
}
