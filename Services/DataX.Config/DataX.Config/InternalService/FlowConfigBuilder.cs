// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using DataX.Config.ConfigDataModel;
using DataX.Contract;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataX.Config.InternalService
{
    [Shared]
    [Export]
    public class FlowConfigBuilder
    {
        [ImportingConstructor]
        public FlowConfigBuilder(FlowDataManager data,
                                IKeyVaultClient keyVaultClient,
                                ConfigGenConfiguration conf,
                                [ImportMany]IEnumerable<IFlowDeploymentProcessor> processors)
        {
            FlowData = data;
            KeyVaultClient = keyVaultClient;
            Configuration = conf;
            Processors = processors.OrderBy(p => p.GetOrder()).ToArray();
        }

        private FlowDataManager FlowData { get; }

        private IKeyVaultClient KeyVaultClient { get; }
        private ConfigGenConfiguration Configuration { get; }
        private IFlowDeploymentProcessor[] Processors { get; }

        public async Task<FlowConfig> Build(FlowGuiConfig inputGuiConfig)
        {
            var defaultConfig = await FlowData.GetDefaultConfig(inputGuiConfig.Input.InputType);
            var config = defaultConfig ?? FlowConfig.From(JsonConfig.CreateEmpty());

            // Set the flowName. This will be the case for new flow creation
            if (string.IsNullOrWhiteSpace(inputGuiConfig.Name))
            {
                string flowName = GenerateValidFlowName(inputGuiConfig.DisplayName);
                config.Name = flowName;
                inputGuiConfig.Name = flowName;
            }
            var guiConfig = await HandleSensitiveData(inputGuiConfig);

            config.Name = guiConfig?.Name ?? config.Name;

            config.DisplayName = guiConfig?.DisplayName ?? config.DisplayName;
            config.Gui = guiConfig.ToJson()._jt;

            return FlowConfig.From(config.ToString());
        }

        private async Task<FlowGuiConfig> HandleSensitiveData(FlowGuiConfig inputConfig)
        {
            await Processors.ChainTask(p => p.HandleSensitiveData(inputConfig));
            return inputConfig;
        }

        private string GenerateValidFlowName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = Guid.NewGuid().ToString().Trim(new[] { '-', '{', '}' });
            }

            // Ensure name contains only Alpha-numeric chars.
            string flowName = Regex.Replace(displayName, "[^A-Za-z0-9]", "").ToLower();

            // Check to make sure the flowName doesn't already exist.
            // The flowName needs to be unique since this name is used to create Azure resources.
            var existingNames = FlowData.GetAll().Result.Where(x => x.Name.Equals(flowName, StringComparison.OrdinalIgnoreCase)).Select(y=>y.Name.ToLower()).ToList();

            if (existingNames.Count !=  0)
            {
               int index = 1;
               var newFlowName = flowName.ToLower() + index.ToString();
                while (existingNames.Contains(newFlowName))
                {
                    index = index++;
                    newFlowName = flowName.ToLower() + index.ToString();
                }
                flowName = newFlowName;
            }

            return flowName;
        }
    }
}
