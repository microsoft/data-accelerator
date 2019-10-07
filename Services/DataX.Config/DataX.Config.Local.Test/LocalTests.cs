// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataX.Config.ConfigDataModel;
using DataX.Config.ConfigGeneration.Processor;
using DataX.Config.Local;
using DataX.Config.PublicService;
using DataX.Config.Utility;
using DataX.Contract;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DataX.Config.Test
{
    [TestClass]
    public class LocalTests
    {
        [ClassInitialize]
        public static void Initialize(TestContext tc)
        {
            InitialConfiguration.Set(Constants.ConfigSettingName_ServiceKeyVaultName, "someservicekeyvault");
            InitialConfiguration.Set(Constants.ConfigSettingName_RuntimeKeyVaultName, "somekeyvault");
            InitialConfiguration.Set(Constants.ConfigSettingName_MetricEventHubConnectionKey, "metric-eventhubconnectionstring");
            InitialConfiguration.Set(Constants.ConfigSettingName_EnableOneBox, "true");
            InitialConfiguration.Set(Constants.ConfigSettingName_ClusterName, "localCluster");
            InitialConfiguration.Set(Constants.ConfigSettingName_ConfigFolderContainerPath, "");
            InitialConfiguration.Set(Constants.ConfigSettingName_ConfigFolderHost, new System.Uri(Environment.CurrentDirectory).AbsoluteUri);
            InitialConfiguration.Set(Constants.ConfigSettingName_LocalMetricsHttpEndpoint, "http://localhost:2020/api/data/upload");

            var conf = new ContainerConfiguration()
                .WithAssembly(typeof(ConfigGenConfiguration).Assembly)
                .WithAssembly(typeof(DataX.Config.Local.LocalDesignTimeStorage).Assembly)
                .WithProvider(new LoggerAndInstanceExportDescriptorProvider<object>(null, new LoggerFactory()));

            CompositionHost = conf.CreateContainer();
            _TestContext = tc;
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

        // [TestCleanup]
        public void TestCleanup()
        {
            // Clean up all the containers in local db
            (DesignTimeStorage as LocalDesignTimeStorage).DropCollection("flows");
            (DesignTimeStorage as LocalDesignTimeStorage).DropCollection("sparkJobTemplates");
            (DesignTimeStorage as LocalDesignTimeStorage).DropCollection("sparkJobs");
            (DesignTimeStorage as LocalDesignTimeStorage).DropCollection("sparkClusters");

            // Cleanup the local db file
            if (File.Exists(LocalDesignTimeStorage._LocalDb))
            {
                File.Delete(LocalDesignTimeStorage._LocalDb);
            }

            // Cleanup local folder created if test passes.
            // If test fails keep it for debugging purpose.
            switch (_TestContext.CurrentTestOutcome)
            {
                case UnitTestOutcome.Passed:
                    {
                        // Delete the runtime folder created
                        if (Directory.Exists(_runtimeConfigFolder))
                        {
                            Directory.Delete(_runtimeConfigFolder, true);
                        }
                        break;
                    }
            }
        }

        public LocalTests()
        {
            CompositionHost.SatisfyImports(this);
            var result = TemplateInitializer.Initialize().Result;
            Ensure.IsSuccessResult(result);
        }

        private static CompositionHost CompositionHost { get; set; }


        [Import]
        private FlowOperation FlowOperation { get; set; }

        [Import]
        private RuntimeConfigGeneration RuntimeConfigGeneration { get; set; }

        [Import]
        private IDesignTimeConfigStorage DesignTimeStorage { get; set; }


        [Import]
        private IRuntimeConfigStorage RuntimeStorage { get; set; }

        [Import]
        private JobOperation JobOperation { get; set; }

        [Import]
        private TemplateInitializer TemplateInitializer { get; set; }

        private static TestContext _TestContext;
        private string _runtimeConfigFolder;


        [TestMethod]
        public async Task TestConfigSaved()
        {
            string flowName = "localconfiggentest";

            var input = await File.ReadAllTextAsync(@"Resource\guiInput.json");
            var inputJson = JsonConfig.From(input);
            var result = await this.FlowOperation.SaveFlowConfig(FlowGuiConfig.From(inputJson));
            Assert.IsTrue(result.IsSuccess, result.Message);

            var actualJson = await this.FlowOperation.GetFlowByName(flowName);
            var expectedJson = FlowConfig.From(await File.ReadAllTextAsync(@"Resource\flowSaved.json"));

            foreach (var match in EntityConfig.Match(expectedJson, actualJson))
            {
                Assert.AreEqual(expected: match.Item2, actual: match.Item3, message: $"path:{match.Item1}");
            }
        }

        [TestMethod]
        public async Task TestEndToEndGeneration()
        {
            var flowName = "localconfiggentest";

            var testingConfig = await File.ReadAllTextAsync(@"Resource\flowSaved.json");
            await DesignTimeStorage.SaveByName(flowName, testingConfig, FlowDataManager.DataCollectionName);

            // generate runtime configs
            var result = await this.RuntimeConfigGeneration.GenerateRuntimeConfigs(flowName);

            var runtimeConfigFolderUri = result.Properties?.GetValueOrDefault(PrepareJobConfigVariables.ResultPropertyName_RuntimeConfigFolder, null);
            _runtimeConfigFolder = new System.Uri(runtimeConfigFolderUri.ToString()).AbsolutePath;

            Assert.IsTrue(result.IsSuccess);

            // verify output schema file is expected
            var expectedSchema = JsonConfig.From(await File.ReadAllTextAsync(@"Resource\schema.json"));
            var p = ResourcePathUtil.Combine(_runtimeConfigFolder, "inputschema.json");
            var pp = new System.Uri(p).AbsolutePath;
            var actualSchema = JsonConfig.From(File.ReadAllText(Path.Combine(_runtimeConfigFolder, "inputschema.json")));
            foreach (var match in JsonConfig.Match(expectedSchema, actualSchema))
            {
                Assert.AreEqual(expected: match.Item2, actual: match.Item3, message: $"path:{match.Item1}");
            }

            // verify output projection file is expected
            var expectedProjection = await File.ReadAllTextAsync(@"Resource\projection.txt");
            var actualProjection = File.ReadAllText(Path.Combine(_runtimeConfigFolder, "projection.txt"));
            Assert.AreEqual(expected: expectedProjection, actual: actualProjection);

            // verify transform file is exepcted
            var expectedTransform = await File.ReadAllTextAsync(@"Resource\localconfiggentest-combined.txt");
            var actualTransform = File.ReadAllText(Path.Combine(_runtimeConfigFolder, "localconfiggentest-combined.txt"));
            Assert.AreEqual(expected: expectedTransform, actual: actualTransform);

            // verify output configuration
            var actualConf = PropertiesDictionary.From(File.ReadAllText(Path.Combine(_runtimeConfigFolder, $"{flowName}.conf")));
            Assert.IsTrue(actualConf.Count == 74, $"Actual entries in .conf not as expected. Expected=65, Got={actualConf.Count}");

            // verify metrics
            var expectedConfig = JsonConfig.From(await File.ReadAllTextAsync(@"Resource\flowStarted.json"));
            var actualConfig = JsonConfig.From(await this.DesignTimeStorage.GetByName(flowName, FlowDataManager.DataCollectionName));

            foreach (var match in JsonConfig.Match(expectedConfig, actualConfig))
            {
                Assert.AreEqual(expected: match.Item2, actual: match.Item3, message: $"path:{match.Item1}");
            }
        }

        // This test needs Spark SDK installed on the machine.
        [Ignore]
        [TestMethod]
        public async Task TestEndToEnd()
        {
            string flowName = "localconfiggentest";

            // Point these two settings to your filesystem locations
            InitialConfiguration.Set(LocalSparkClient.ConfigSettingName_SparkHomeFolder, @"<LOCAL SPARK HOME FOLDER>");
            InitialConfiguration.Set(Constants.ConfigSettingName_LocalRoot, @"<LOCAL ROOT>");

            var input = await File.ReadAllTextAsync(@"Resource\guiInputLocal.json");
            var inputJson = JsonConfig.From(input);
            var result = await this.FlowOperation.SaveFlowConfig(FlowGuiConfig.From(inputJson));
            Assert.IsTrue(result.IsSuccess, result.Message);

            var result2 = await this.RuntimeConfigGeneration.GenerateRuntimeConfigs(flowName);
            var runtimeConfigFolderUri = result2.Properties?.GetValueOrDefault(PrepareJobConfigVariables.ResultPropertyName_RuntimeConfigFolder, null);
            _runtimeConfigFolder = new System.Uri(runtimeConfigFolderUri.ToString()).AbsolutePath;

            var jobResult1 = await JobOperation.StartJob(flowName);
            Assert.IsTrue(jobResult1.IsSuccess);
            // Wait for few seconds for the job to do some work
            System.Threading.Thread.Sleep(5000);
            var jobResult2 = await JobOperation.StopJob(flowName);
            Assert.IsTrue(jobResult2.IsSuccess);
        }
                
        [TestMethod]
        public async Task TestDelete()
        {
            string flowName = "localconfiggentest";

            var testingConfig = await File.ReadAllTextAsync(@"Resource\flowSaved.json");
            await DesignTimeStorage.SaveByName(flowName, testingConfig, FlowDataManager.DataCollectionName);

            // generate runtime configs
            var result = await this.RuntimeConfigGeneration.GenerateRuntimeConfigs(flowName);

            var runtimeConfigFolderUri = result.Properties?.GetValueOrDefault(PrepareJobConfigVariables.ResultPropertyName_RuntimeConfigFolder, null);
            _runtimeConfigFolder = new System.Uri(runtimeConfigFolderUri.ToString()).AbsolutePath;

            Assert.IsTrue(result.IsSuccess);

            // Invoke delete
            var runtimeDeleteResult = await RuntimeConfigGeneration.DeleteConfigs(flowName);
            Ensure.IsSuccessResult(runtimeDeleteResult);

            // Ensure flow config doesn't exist anymore
            var flowConfigs = await this.DesignTimeStorage.GetAll(FlowDataManager.DataCollectionName);
            Assert.IsTrue(flowConfigs.Count()==0);

            // Ensure job config doesn't exist anymore
            var jobConfigs = await this.DesignTimeStorage.GetAll(SparkJobData.DataCollectionName);
            Assert.IsTrue(jobConfigs.Count() == 0);

            // Ensure runtime configs folder doesn't exist anymore
            Assert.IsTrue(!Directory.Exists(_runtimeConfigFolder));
        }
    }
}
