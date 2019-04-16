// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataX.Flow.Common;

namespace DataX.Flow.Generator.Tests
{
    [TestClass]
    public class GeneratorTests
    {
        [TestMethod]
        public void FlowIdSymbolsTest()
        {
            var flowId = EngineEnvironment.GenerateValidFlowId("flow-TEST_name_123_!@");
            Assert.AreEqual("flowtestname123", flowId, "flowId includes invalid chars");
        }

        [TestMethod]
        public void FlowIdEmptyTest()
        {
            var flowId = EngineEnvironment.GenerateValidFlowId("");
            Assert.AreNotEqual("", flowId, "flowId is empty");
        }
    }
}
