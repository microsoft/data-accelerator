// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using DataX.Config.LivyClient.Test.Mock;
using System.Threading.Tasks;

namespace DataX.Config.LivyClient.Test
{
    [TestClass]
    public class JobOperationTest
    {
        [TestMethod]
        public async Task TestStartJob()
        {
            var appInfoJson = "\"appInfo\":{\"driverLogUrl\":\"http://123.xx.internal.cloudapp.net:3/node/containerlogs/container_123/livy\",\"sparkUiUrl\":\"https://site.azurehdinsight.net/yarnui/hn/proxy/application_123_0101/\"}";
            var logs1 = ",\"log\":[\"\\t user: livy\",\"19/01/30 04:02:31 INFO ShutdownHookManager: Shutdown hook called\",\"\\nYARN Diagnostics: \"]}";
            var logs2 =",\"log\":[\"\\t user: livy\",\"19/01/30 04:02:31 INFO ShutdownHookManager: Shutdown hook called\",\"19/01/30 04:02:31 INFO ShutdownHookManager: Deleting directory /tmp/spark-123\",\"19/01/30 04:02:31 INFO ShutdownHookManager: Deleting directory /tmp/spark-123\",\"19/01/30 04:02:31 INFO MetricsSystemImpl: Stopping azure-file-system metrics system...\",\"19/01/30 04:02:31 INFO MetricsSinkAdapter: azurefs2 thread interrupted.\",\"19/01/30 04:02:31 INFO MetricsSystemImpl: azure-file-system metrics system stopped.\",\"19/01/30 04:02:31 INFO MetricsSystemImpl: azure-file-system metrics system shutdown complete.\",\"\\nstderr: \",\"\\nYARN Diagnostics: \"]}";

            var httpClientFactory = new HttpClientFactory();
            httpClientFactory
                .AddResponse(HttpMethod.Post, "http://localhost/batches", "{\"id\":999,\"state\":\"starting\",\"appId\":\"application_123_0101\"," + appInfoJson + logs1)
                .AddResponse(HttpMethod.Get, "http://localhost/batches/999", "{\"id\":999,\"state\":\"running\",\"appId\":\"application_123_0101\"," + appInfoJson + logs2);
                
            var livyClientFactory = new LivyClientFactory(httpClientFactory);
            var livyClient = await livyClientFactory.GetClient("endpoint=http://localhost;username=test;password=test");
            var jobInfo = await livyClient.SubmitJob(JObject.Parse("{}"));

            Assert.AreEqual(expected: "999", actual: jobInfo.JobId);
            Assert.AreEqual(expected: JobState.Starting, actual: jobInfo.JobState);

            var refreshJobInfo = await livyClient.GetJobInfo(jobInfo.ClientCache);
            Assert.AreEqual(expected: "999", actual: refreshJobInfo.JobId);
            Assert.AreEqual(expected: JobState.Running, actual: refreshJobInfo.JobState);
        }

        [TestMethod]
        public async Task TestGetJobs()
        {
            var appInfoJson = "\"appInfo\":{\"driverLogUrl\":\"http://123.xx.internal.cloudapp.net:3/node/containerlogs/container_123/livy\",\"sparkUiUrl\":\"https://site.azurehdinsight.net/yarnui/hn/proxy/application_123_0101/\"}";
            var logs1 = ",\"log\":[\"\\t user: livy\",\"19/01/30 04:02:31 INFO ShutdownHookManager: Shutdown hook called\",\"\\nYARN Diagnostics: \"]}";
            var logs2 = ",\"log\":[\"\\t user: livy\",\"19/01/30 04:02:31 INFO ShutdownHookManager: Shutdown hook called\",\"19/01/30 04:02:31 INFO ShutdownHookManager: Deleting directory /tmp/spark-123\",\"19/01/30 04:02:31 INFO ShutdownHookManager: Deleting directory /tmp/spark-123\",\"19/01/30 04:02:31 INFO MetricsSystemImpl: Stopping azure-file-system metrics system...\",\"19/01/30 04:02:31 INFO MetricsSinkAdapter: azurefs2 thread interrupted.\",\"19/01/30 04:02:31 INFO MetricsSystemImpl: azure-file-system metrics system stopped.\",\"19/01/30 04:02:31 INFO MetricsSystemImpl: azure-file-system metrics system shutdown complete.\",\"\\nstderr: \",\"\\nYARN Diagnostics: \"]}";

            var httpClientFactory = new HttpClientFactory();
            httpClientFactory
                .AddResponse(HttpMethod.Get, "http://localhost/batches", "{\"from\":\"0\",\"total\":\"2\"," + "\"sessions\":["
                                        + "{\"id\":999,\"state\":\"starting\",\"appId\":\"application_123_0101\"," + appInfoJson + logs1 + ","
                                        + "{\"id\":9991,\"state\":\"starting\",\"appId\":\"application_123_0102\"," + appInfoJson + logs2 + ""
                                        + "]}");

            var livyClientFactory = new LivyClientFactory(httpClientFactory);
            var livyClient = await livyClientFactory.GetClient("endpoint=http://localhost;username=test;password=test");
            var jobInfo = await livyClient.GetJobs();

            Assert.AreEqual(expected: 2, actual: jobInfo.Length);

            Assert.AreEqual(expected: JobState.Starting, actual: jobInfo[0].JobState);
            Assert.AreEqual(expected: "999", actual: jobInfo[0].JobId);

            Assert.AreEqual(expected: JobState.Starting, actual: jobInfo[1].JobState);
            Assert.AreEqual(expected: "9991", actual: jobInfo[1].JobId);
        }

        [TestMethod]
        public async Task TestStopJob()
        {
            var appInfoJson = "\"appInfo\":{\"driverLogUrl\":\"http://123.xx.internal.cloudapp.net:3/node/containerlogs/container_123/livy\",\"sparkUiUrl\":\"https://site.azurehdinsight.net/yarnui/hn/proxy/application_123_0101/\"}";
            var logs1 = ",\"log\":[\"\\t user: livy\",\"19/01/30 04:02:31 INFO ShutdownHookManager: Shutdown hook called\",\"\\nYARN Diagnostics: \"]}";
            
            var httpClientFactory = new HttpClientFactory();
            httpClientFactory
                .AddResponse(HttpMethod.Delete, "http://localhost/batches/0", "{\"id\":2,\"state\":\"starting\",\"appId\":\"application_123_0101\"," + appInfoJson + logs1);

            var livyClientFactory = new LivyClientFactory(httpClientFactory);
            var livyClient = await livyClientFactory.GetClient("endpoint=http://localhost;username=test;password=test");

            var jobDelete = await livyClient.StopJob(JObject.Parse("{}"));

            Assert.AreEqual(expected: null, actual: jobDelete.JobId);
        }

        [TestMethod]
        public async Task TestJobNotFound()
        {
            var appInfoJson = "\"appInfo\":{\"driverLogUrl\":\"http://123.xx.internal.cloudapp.net:3/node/containerlogs/container_123/livy\",\"sparkUiUrl\":\"https://site.azurehdinsight.net/yarnui/hn/proxy/application_123_0101/\"}";
            var logs1 = ",\"log\":[\"\\t user: livy\",\"19/01/30 04:02:31 INFO ShutdownHookManager: Shutdown hook called\",\"\\nYARN Diagnostics: \"]}";
            
            var httpClientFactory = new HttpClientFactory();
            httpClientFactory
                .AddResponse(HttpMethod.Post, "http://localhost/batches", "{\"id\":2,\"state\":\"starting\",\"appId\":\"application_123_0101\"," + appInfoJson + logs1)
                .AddResponse(HttpMethod.Get, "http://localhost/batches/2", "Session '2' not found.", System.Net.HttpStatusCode.NotFound);

            var livyClientFactory = new LivyClientFactory(httpClientFactory);
            var livyClient = await livyClientFactory.GetClient("endpoint=http://localhost;username=test;password=test");
            var jobInfo = await livyClient.SubmitJob(JObject.Parse("{}"));

            Assert.AreEqual(expected: "2", actual: jobInfo.JobId);
            Assert.AreEqual(expected: JobState.Starting, actual: jobInfo.JobState);

            var refreshJobInfo = await livyClient.GetJobInfo(jobInfo.ClientCache);
            Assert.AreEqual(expected: null, actual: refreshJobInfo.JobId);
            Assert.AreEqual(expected: JobState.Idle, actual: refreshJobInfo.JobState);
            Assert.AreEqual(expected: null, actual: refreshJobInfo.ClientCache);
            Assert.AreEqual(expected: "Session '2' not found.", actual: refreshJobInfo.Note);
            Assert.AreEqual(expected: null, actual: refreshJobInfo.Links);
        }
    }
}
