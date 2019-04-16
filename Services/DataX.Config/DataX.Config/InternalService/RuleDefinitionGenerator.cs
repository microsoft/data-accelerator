// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json.Linq;
using DataX.Config.ConfigDataModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataX.Config
{
    public static class RuleDefinitionGenerator
    {
        public static string GenerateRuleDefinitions(IEnumerable<FlowGuiRule> ruleDefinitionList, string name)
        {
            JArray rules = new JArray();
            foreach (var rule in ruleDefinitionList)
            {
                var ruleId = rule.Properties.Value<string>("$ruleId");
                if (string.IsNullOrEmpty(ruleId))
                {
                    rule.Properties["$ruleId"] = Guid.NewGuid();
                }

                rule.Properties["$productId"] = name;

                rules.Add(rule.Properties);
            }

            var ruleDefinitions = new JArray() { ruleDefinitionList.Select(r => r.Properties) };
            return ruleDefinitions.ToString();
        }

    }
}
