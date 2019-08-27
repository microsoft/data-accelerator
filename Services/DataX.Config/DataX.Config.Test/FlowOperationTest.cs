// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using DataX.Config.ConfigDataModel;
using DataX.Config.PublicService;
using DataX.Config.Test.Extension;
using DataX.Config.Test.Mock;
using DataX.Config.Test.Utility.Mock;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DataX.Config.Utility;

namespace DataX.Config.Test
{
    [TestClass]
    public class FlowOperationTest
    {
        [ClassInitialize]
        public static void Initialize(TestContext tc)
        {
            InitialConfiguration.Set(Constants.ConfigSettingName_RuntimeKeyVaultName, "somekeyvault");

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

        public FlowOperationTest()
        {
            CompositionHost.SatisfyImports(this);
        }

        private static CompositionHost CompositionHost { get; set; }

        [Import]
        private FlowOperation FlowOperation { get; set; }

        [Import]
        private ICommonDataManager CommonData { get; set; }
                
        [TestMethod]
        public async Task TestConfigSaved()
        {
            await CommonData.Add(FlowDataManager.CommonDataName_DefaultFlowConfig, @"Resource\flowDefault.json");

            var input = await File.ReadAllTextAsync(@"Resource\guiInput.json");
            var inputJson = JsonConfig.From(input);
            var result = await this.FlowOperation.SaveFlowConfig(FlowGuiConfig.From(inputJson));
            Assert.IsTrue(result.IsSuccess, result.Message);

            var actualJson = await this.FlowOperation.GetFlowByName("configgentest");
            var expectedJson = FlowConfig.From(await File.ReadAllTextAsync(@"Resource\flowSaved.json"));

            foreach(var match in EntityConfig.Match(expectedJson, actualJson))
            {
                Assert.AreEqual(expected: match.Item2, actual: match.Item3, message: $"path:{match.Item1}");
            }

            Cleanup();
        }
    }
}
