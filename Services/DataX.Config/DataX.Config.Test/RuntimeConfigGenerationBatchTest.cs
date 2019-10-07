// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataX.Config.ConfigDataModel;
using DataX.Config.ConfigGeneration.Processor;
using DataX.Config.Test.Extension;
using DataX.Config.Test.Mock;
using DataX.Config.Test.Utility.Mock;
using DataX.Config.Utility;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace DataX.Config.Test
{
    [TestClass]
    public class RuntimeConfigGenerationBatchTest
    {
        [ClassInitialize]
        public static void Initialize(TestContext tc)
        {
            InitialConfiguration.Set(Constants.ConfigSettingName_ClusterName, "somecluster");
            InitialConfiguration.Set(Constants.ConfigSettingName_ServiceKeyVaultName, "someservicekeyvault");
            InitialConfiguration.Set(Constants.ConfigSettingName_RuntimeKeyVaultName, "somekeyvault");
            InitialConfiguration.Set(Constants.ConfigSettingName_MetricEventHubConnectionKey, "metric-eventhubconnectionstring");

            var conf = new ContainerConfiguration()
                .WithAssembly(typeof(ConfigGenConfiguration).Assembly)
                .WithAssembly(typeof(MockBase).Assembly)
                .WithAssembly(Assembly.GetExecutingAssembly())
                .WithProvider(new LoggerAndInstanceExportDescriptorProvider<object>(null, new LoggerFactory()));

            CompositionHost = conf.CreateContainer();
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            if (CompositionHost != null)
            {
                CompositionHost.Dispose();
                CompositionHost = null;
            }

            InitialConfiguration.Clear();
        }

        public RuntimeConfigGenerationBatchTest()
        {
            CompositionHost.SatisfyImports(this);
        }

        private static CompositionHost CompositionHost { get; set; }

        [Import]
        private RuntimeConfigGeneration RuntimeConfigGeneration { get; set; }

        [Import]
        private DesignTimeStorage DesignTimeStorage { get; set; }

        [Import]
        private RuntimeStorage RuntimeStorage { get; set; }

        [Import]
        private ICommonDataManager CommonData { get; set; }
        
        [Import]
        private ConfigurationProvider ConfigurationProvider { get; set; }

        [Shared]
        [Export(typeof(IFlowDeploymentProcessor))]
        private class VerifyJsonConfigGenerated : ProcessorBase
        {
            public override int GetOrder()
            {
                // set an order number to be placed right after the GenerateJobConfig processor
                return 601;
            }

            public override async Task<string> Process(FlowDeploymentSession flowToDeploy)
            {
                var guiConfig = flowToDeploy.Config.GetGuiConfig();
                if (guiConfig?.Input?.Mode == Constants.InputMode_Batching)
                {
                    var jobConfigs = flowToDeploy.GetJobs().First().JobConfigs;
                    var reConfigs = jobConfigs.Where(j => !j.IsOneTime).ToList();
                    var otConfigs = jobConfigs.Where(j => j.IsOneTime).ToList();

                    Assert.AreEqual(expected: 2, actual: reConfigs.Count);
                    Assert.AreEqual(expected: 3, actual: otConfigs.Count);

                    var startTimeConfig = (DateTime)guiConfig.BatchList[0].Properties.StartTime;
                    var curTime = startTimeConfig.AddDays(1);
                    var normalizedStartTime = startTimeConfig.Add(-startTimeConfig.TimeOfDay);

                    TimeSpan interval = new TimeSpan(1, 0, 0, 0);
                    var expected = normalizedStartTime;
                    foreach(var c in reConfigs)
                    {
                        var actualStart = DateTime.Parse(c.ProcessStartTime).ToUniversalTime();
                        var actualEnd = DateTime.Parse(c.ProcessEndTime).ToUniversalTime();
                        var window = (actualEnd - actualStart).TotalSeconds;
                        Assert.AreEqual(expected: expected.AddDays(-2), actual: actualStart, message: $"StartTime:{actualStart}");
                        Assert.AreEqual(expected: expected.AddDays(1).AddSeconds(-1), actual: actualEnd, message: $"EndTime:{actualEnd}");
                        Assert.AreEqual(expected: 259199, actual: window, message: $"Window:{window}");
                        Assert.IsTrue(actualStart < curTime);
                        expected = expected.Add(interval);
                    }

                    var lastScheduleTime = DateTime.Parse(reConfigs.Last().ProcessStartTime).ToUniversalTime();
                    Assert.IsTrue(lastScheduleTime < curTime);

                    expected = normalizedStartTime;
                    foreach (var c in otConfigs)
                    {
                        var actualStart = DateTime.Parse(c.ProcessStartTime).ToUniversalTime();
                        var actualEnd = DateTime.Parse(c.ProcessEndTime).ToUniversalTime();
                        var window = (actualEnd - actualStart).TotalSeconds;
                        Assert.AreEqual(expected: expected, actual: actualStart, message: $"StartTime:{actualStart}");
                        Assert.AreEqual(expected: expected.AddDays(1).AddSeconds(-1), actual: actualEnd, message: $"EndTime:{actualEnd}");
                        Assert.AreEqual(expected: 86399, actual: window, message: $"Window:{window}");
                        expected = expected.Add(interval);
                    }

                    lastScheduleTime = DateTime.Parse(otConfigs.Last().ProcessStartTime).ToUniversalTime();
                    Assert.IsTrue(lastScheduleTime > curTime);
                }

                return "done";
            }
        }
        [TestMethod]
        public async Task TestBatchSchedule()
        {
            var flowName = "configgenbatchtest";

            var testingConfig = await File.ReadAllTextAsync(@"Resource\batchFlow.json");
            var current = DateTime.UtcNow;

            var startTimeConfig = current.AddDays(-1);
            var endTimeConfig = current.AddDays(1);
            var normalizedStartTime = startTimeConfig.Add(-startTimeConfig.TimeOfDay);

            testingConfig = testingConfig.Replace("${startTime}", startTimeConfig.ToString("o"));
            testingConfig = testingConfig.Replace("${endTime}", endTimeConfig.ToString("o"));

            await DesignTimeStorage.SaveByName(flowName, testingConfig, FlowDataManager.DataCollectionName);

            await CommonData.Add("defaultJobTemplate", @"Resource\batchSparkJobTemplate.json");
            await CommonData.Add(ConfigFlattenerManager.DefaultConfigName, @"Resource\flattenerConfig.json");
            await CommonData.Add(FlowDataManager.CommonDataName_DefaultFlowConfig, @"Resource\batchFlowDefault.json");

            var result = await this.RuntimeConfigGeneration.GenerateRuntimeConfigs(flowName);

            var runtimeConfigFolder = result.Properties?.GetValueOrDefault(PrepareJobConfigVariables.ResultPropertyName_RuntimeConfigFolder, null);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(expected: 8, actual: RuntimeStorage.Cache.Count);

            var processTime = Regex.Replace(normalizedStartTime.ToString("s"), "[^0-9]", "");

            var actualConf = PropertiesDictionary.From(this.RuntimeStorage.Cache[ResourcePathUtil.Combine(runtimeConfigFolder.ToString() + $@"/Recurring/{processTime}", $"{flowName}.conf")]);
            var conf = await File.ReadAllTextAsync(@"Resource\batchJobConfig.conf");
            conf = conf.Replace("${processTime}", processTime);
            conf = conf.Replace("${startTime}", normalizedStartTime.AddDays(-2).ToString("o"));
            conf = conf.Replace("${endTime}", normalizedStartTime.AddDays(1).AddSeconds(-1).ToString("o"));
            var expectedConf = PropertiesDictionary.From(conf);

            var matches = PropertiesDictionary.Match(expectedConf, actualConf).ToList();
            foreach (var match in matches)
            {
                Console.WriteLine($"prop:{match.Item1 ?? "null"}, expected:<{match.Item2 ?? "null"}>, actual:<{match.Item3 ?? "null"}>");
            }

            foreach (var match in matches)
            {
                Assert.AreEqual(expected: match.Item2, actual: match.Item3, message: $"property:{match.Item1}");
            }

            Cleanup();
        }
    }
}
