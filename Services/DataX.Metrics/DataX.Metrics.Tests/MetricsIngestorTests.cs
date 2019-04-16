// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataX.Metrics.Ingestor.Helper;
using System;

namespace DataX.Metrics.Tests
{
    [TestClass]
    public class MetricsIngestorTests
    {
        [TestMethod]
        public void TestLongTime()
        {
            string input = "{\"val\":0.998,\"met\":\"sensor1\",\"app\":\"MyFlow\",\"uts\":1542322572}";

            var metricOutput = IngestorEventProcessor.GenerateRow(input);
            Assert.AreEqual("MyFlow:sensor1", metricOutput.RedisKey);

            Assert.IsTrue(metricOutput.Content.StartsWith("{\"uts\":15"));//data is in this year
            Assert.IsTrue(metricOutput.Content.Length == 47);//contains miliseconds
            Assert.IsTrue(metricOutput.Content.EndsWith("\"val\":0.998, \"pivot1\":\"\"}"));

            DateTime epochZero = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            Assert.IsTrue(1542322572000 < metricOutput.EpochTime && metricOutput.EpochTime < 1600000000000);//event time goes against DateTime.Now, so checking is it right format and recent
        }

        [TestMethod]
        public void TestTimestampTime()
        {
            string input = "{\"val\":0.998,\"met\":\"sensor1\",\"app\":\"MyFlow\",\"uts\":\"2018-11-15T03:27:46.285Z\"}";

            var metricOutput = IngestorEventProcessor.GenerateRow(input);
            Assert.AreEqual("MyFlow:sensor1", metricOutput.RedisKey);

            Assert.IsTrue(metricOutput.Content.StartsWith("{\"uts\":15"));
            Assert.IsTrue(metricOutput.Content.Length == 47);//contains miliseconds
            Assert.IsTrue(metricOutput.Content.EndsWith("\"val\":0.998, \"pivot1\":\"\"}"));

            DateTime epochZero = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            Assert.IsTrue(1542322572000 < metricOutput.EpochTime && metricOutput.EpochTime < 1600000000000);//event time goes against DateTime.Now, so checking is it right format and recent
        }

        [TestMethod]
        public void TestPivotValue()
        {
            string input = "{\"Metric\":0.998,\"MetricName\":\"sensor1\",\"Product\":\"MyFlow\",\"EventTime\":\"2018-11-15T03:27:46.285Z\",\"Pivot1\":\"text\"}";

            var metricOutput = IngestorEventProcessor.GenerateRow(input);
            Assert.AreEqual("MyFlow:sensor1", metricOutput.RedisKey);

            Assert.IsTrue(metricOutput.Content.StartsWith("{\"uts\":15"));
            Assert.IsTrue(metricOutput.Content.Length == 51);//contains miliseconds
            Assert.IsTrue(metricOutput.Content.EndsWith("\"val\":0.998, \"pivot1\":\"text\"}"));

            DateTime epochZero = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            Assert.IsTrue(1542322572000 < metricOutput.EpochTime && metricOutput.EpochTime < 1600000000000);//event time goes against DateTime.Now, so checking is it right format and recent

        }
    }
}
