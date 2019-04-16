// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataX.Config.Test.Utility.Mock;
using System.Composition;
using System.Composition.Hosting;

namespace DataX.Config.Test
{
    [TestClass]
    public class LoggerTest
    {
        [ClassInitialize]
        public static void Initialize(TestContext tc)
        {
            var conf = new ContainerConfiguration()
                .WithAssembly(typeof(ConfigGenConfiguration).Assembly)
                .WithPart<CacheLogger>();

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

        public LoggerTest()
        {
            CompositionHost.SatisfyImports(this);
        }

        private static CompositionHost CompositionHost { get; set; }

        [Import]
        private ILogger Logger { get; set; }

        [Import]
        private CacheLogger InternalLogger { get; set; }


        [TestMethod]
        public void TestLogsGenerated()
        {
            Logger.LogInformation("testevent/foo/bar");

            Assert.AreEqual(expected: 1, actual: InternalLogger.Logs.Count);
            Assert.AreEqual(expected: "Information-0: testevent/foo/bar", actual: InternalLogger.Logs[0]);
        }
    }
}
