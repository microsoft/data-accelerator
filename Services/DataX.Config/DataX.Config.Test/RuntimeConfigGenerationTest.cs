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

namespace DataX.Config.Test
{
    [TestClass]
    public class RuntimeConfigGenerationTest
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

        public RuntimeConfigGenerationTest()
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
                if(flowToDeploy.Name == "configgentest")
                {
                    var expectedConfigContent = await File.ReadAllTextAsync(@"Resource\jobConfig.json");
                    var expectedJson = JsonConfig.From(expectedConfigContent);
                    var actualContentContent = flowToDeploy.GetJobs().First().GetTokenString(GenerateJobConfig.TokenName_JobConfigContent);
                    var actualJson = JsonConfig.From(actualContentContent);

                    foreach (var match in JsonConfig.Match(expectedJson, actualJson))
                    {
                        Assert.AreEqual(expected: match.Item2, actual: match.Item3, message: $"path:{match.Item1}");
                    }
                }
                return "done";
            }
        }

        [TestMethod]
        public async Task TestEndToEndGeneration()
        {
            var flowName = "configgentest";

            var testingConfig = await File.ReadAllTextAsync(@"Resource\flowSaved.json");
            await DesignTimeStorage.SaveByName(flowName, testingConfig,  FlowDataManager.DataCollectionName);

            await CommonData.Add("defaultJobTemplate", @"Resource\sparkJobTemplate.json");
            await CommonData.Add(ConfigFlattenerManager.DefaultConfigName, @"Resource\flattenerConfig.json");
            await CommonData.Add(FlowDataManager.CommonDataName_DefaultFlowConfig, @"Resource\flowDefault.json");

            var result = await this.RuntimeConfigGeneration.GenerateRuntimeConfigs(flowName);

            var runtimeConfigFolder = result.Properties?.GetValueOrDefault(PrepareJobConfigVariables.ResultPropertyName_RuntimeConfigFolder, null);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(expected: 4, actual: RuntimeStorage.Cache.Count);

            // verify output schema file is expected
            var expectedSchema = JsonConfig.From(await File.ReadAllTextAsync(@"Resource\schema.json"));
            var actualSchema = JsonConfig.From(RuntimeStorage.Cache[ResourcePathUtil.Combine(runtimeConfigFolder.ToString(), "inputschema.json")]);
            foreach (var match in JsonConfig.Match(expectedSchema, actualSchema))
            {
                Assert.AreEqual(expected: match.Item2, actual: match.Item3, message: $"path:{match.Item1}");
            }

            // verify output projection file is expected
            var expectedProjection= await File.ReadAllTextAsync(@"Resource\projection.txt");
            var actualProjection = RuntimeStorage.Cache[ResourcePathUtil.Combine(runtimeConfigFolder.ToString(), "projection.txt")];
            Assert.AreEqual(expected: expectedProjection, actual: actualProjection);

            // verify transform file is exepcted
            var expectedTransform = await File.ReadAllTextAsync(@"Resource\configgentest-combined.txt");
            var actualTransform = RuntimeStorage.Cache[ResourcePathUtil.Combine(runtimeConfigFolder.ToString(), "configgentest-combined.txt")];
            Assert.AreEqual(expected: expectedTransform, actual: actualTransform);

            // Verify output configuration is expected
            var actualConf = PropertiesDictionary.From(this.RuntimeStorage.Cache[ResourcePathUtil.Combine(runtimeConfigFolder.ToString(), $"{flowName}.conf")]);
            var expectedConf = PropertiesDictionary.From(await File.ReadAllTextAsync(@"Resource\jobConfig.conf"));
            var matches = PropertiesDictionary.Match(expectedConf, actualConf).ToList();
            foreach (var match in matches)
            {
                Console.WriteLine($"prop:{match.Item1 ?? "null"}, expected:<{match.Item2 ?? "null"}>, actual:<{match.Item3 ?? "null"}>");
            }

            foreach (var match in matches)
            {
                Assert.AreEqual(expected: match.Item2, actual: match.Item3, message: $"property:{match.Item1}");
            }

            // Verify metrics
            var expectedConfig = JsonConfig.From(await File.ReadAllTextAsync(@"Resource\flowStarted.json"));
            var actualConfig = JsonConfig.From(await this.DesignTimeStorage.GetByName(flowName, FlowDataManager.DataCollectionName));
                        
            foreach (var match in JsonConfig.Match(expectedConfig, actualConfig))
            {
                Assert.AreEqual(expected: match.Item2, actual: match.Item3, message: $"path:{match.Item1}");
            }

            Cleanup();
        }
    }
}
