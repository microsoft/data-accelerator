// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataX.Config.ConfigDataModel;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DataX.Config.Test
{
    [TestClass]
    public class RuleDefinitionGeneratorTest
    {
        [TestMethod]
        public void GenerateRuleDefinitionsTest()
        {
            var props = @"{$productId: '', $ruleType: 'SimpleRule', $ruleId: '', $ruleDescription: 'test'}";
            var ruleDefinitionList = new List<FlowGuiRule>
            {
                new FlowGuiRule()
                {
                    Id = "test",
                    Type = "test",
                    Properties = JObject.Parse(props)
                }
            };

            var actual = JArray.Parse(RuleDefinitionGenerator.GenerateRuleDefinitions(ruleDefinitionList, "test123"));

            Assert.AreEqual("test123", actual[0].Value<string>("$productId"));
            Assert.IsTrue(Guid.TryParse(actual[0].Value<string>("$ruleId"), out Guid newGuid));

            props = @"{$productId: '', $ruleType: 'SimpleRule', $ruleId: '12345', $ruleDescription: 'test'}";
            ruleDefinitionList = new List<FlowGuiRule>
            {
                new FlowGuiRule()
                {
                    Id = "test",
                    Type = "test",
                    Properties = JObject.Parse(props)
                }
            };

            actual = JArray.Parse(RuleDefinitionGenerator.GenerateRuleDefinitions(ruleDefinitionList, "test123"));

            Assert.AreEqual("test123", actual[0].Value<string>("$productId"));
            Assert.AreEqual("12345", actual[0].Value<string>("$ruleId"));
        }
    }
}
