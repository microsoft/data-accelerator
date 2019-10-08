// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataX.Config.Test.Mock;
using DataX.Config.Test.Utility.Mock;
using System;
using System.Composition;
using System.Composition.Hosting;
using System.Reflection;
using DataX.Config.Utility;

namespace DataX.Config.Test
{
    [TestClass]
    public class CompositionTest
    {
        [ClassInitialize]
        public static void Initialize(TestContext tc)
        {
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
        }

        public CompositionTest()
        {
            CompositionHost.SatisfyImports(this);
        }

        private static CompositionHost CompositionHost { get; set; }

        [Import]
        private RuntimeConfigGeneration RuntimeConfigGeneration { get; set; }

        [Import]
        private ConfigGenConfiguration Configuration { get; set; }

        [Import]
        private ConfigurationProvider ConfigurationProvider { get; set; }

        [TestMethod]
        public void TestConfigurationImported()
        {
            Assert.IsNotNull(this.RuntimeConfigGeneration);
            Assert.IsInstanceOfType(this.RuntimeConfigGeneration, typeof(RuntimeConfigGeneration));

            ConfigurationProvider.Add("foo", "bar");
            Assert.IsNotNull(this.Configuration);
            Assert.AreEqual("bar", this.Configuration["foo"]);
        }
    }
}
