// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Config.Utility;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.ConfigGeneration.Processor
{
    /// <summary>
    /// Merge default flow config
    /// </summary>
    [Shared]
    [Export(typeof(IFlowDeploymentProcessor))]
    public class MergeDefaultConfig : ProcessorBase
    {
        public const string AttachementName_DefaultFlowConfig = "defaultFlowConfig";

        [ImportingConstructor]
        public MergeDefaultConfig(FlowDataManager flowData)
        {
            this.FlowData = flowData;
        }

        private FlowDataManager FlowData { get; }

        public override int GetOrder()
        {
            return 200;
        }

        public override async Task<string> Process(FlowDeploymentSession flowToDeploy)
        {
            var flowConfig = flowToDeploy.Config;

            var defaultConfig = await FlowData.GetDefaultConfig(flowConfig.GetGuiConfig().Input.InputType, flowToDeploy.Tokens);
            if (defaultConfig == null)
            {
                return "defaultConfig is null, skipped";
            }

            var newConfig = flowConfig.RebaseOn(defaultConfig);
            flowToDeploy.UpdateFlowConfig(newConfig);
            flowToDeploy.SetAttachment(AttachementName_DefaultFlowConfig, defaultConfig);

            return "done";
        }
    }
}
