// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Config.ConfigGeneration.Processor;
using DataX.Config.Test.Extension;
using DataX.Config.Test.Mock;
using DataX.Config.Test.Utility.Mock;
using DataX.Config.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DataX.Config.Test
{
    [TestClass]
    public class RuntimeConfigGenerationTestCustom
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

        public RuntimeConfigGenerationTestCustom()
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
                return await Task.FromResult("done");
            }
        }

        [TestMethod]
        public async Task EndToEndGenerationCustom()
        {
            var flowName = "customconfiggentest";

            var testingConfig = await File.ReadAllTextAsync(@"Resource\customflow.json");
            await DesignTimeStorage.SaveByName(flowName, testingConfig, FlowDataManager.DataCollectionName);

            await CommonData.Add("defaultFlowConfig", @"Resource\customflow.json");
            await CommonData.Add("flattener", @"Resource\customFlattenerConfig.json");
            await CommonData.Add("defaultJobTemplate", @"Resource\sparkJobTemplate.json");

            var result = await this.RuntimeConfigGeneration.GenerateRuntimeConfigs(flowName);

            var runtimeConfigFolder = result.Properties?.GetValueOrDefault(PrepareJobConfigVariables.ResultPropertyName_RuntimeConfigFolder, null);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(expected: 2, actual: RuntimeStorage.Cache.Count);

            var jobConfigDestinationFolder = runtimeConfigFolder?.ToString().Split("Generation_").First();

            // Verify output configuration is expected
            var actualConf = PropertiesDictionary.From(this.RuntimeStorage.Cache[ResourcePathUtil.Combine(runtimeConfigFolder.ToString(), "customconfiggentest1.conf")]);
            var expectedConf = PropertiesDictionary.From(await File.ReadAllTextAsync(@"Resource\customJobConfig1.conf"));
            var matches = PropertiesDictionary.Match(expectedConf, actualConf).ToList();
            foreach (var match in matches)
            {
                Console.WriteLine($"prop:{match.Item1 ?? "null"}, expected:<{match.Item2 ?? "null"}>, actual:<{match.Item3 ?? "null"}>");
            }

            foreach (var match in matches)
            {
                Assert.AreEqual(expected: match.Item2, actual: match.Item3, message: $"property:{match.Item1}");
            }

            //verify second job conf is generated as expected
            flowName = "customconfiggentest2";
            actualConf = PropertiesDictionary.From(this.RuntimeStorage.Cache[ResourcePathUtil.Combine(runtimeConfigFolder.ToString(), "customconfiggentest2.conf")]);
            expectedConf = PropertiesDictionary.From(await File.ReadAllTextAsync(@"Resource\customJobConfig2.conf"));
            matches = PropertiesDictionary.Match(expectedConf, actualConf).ToList();
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
