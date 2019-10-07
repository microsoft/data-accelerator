// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using DataX.Config.ConfigDataModel.RuntimeConfig;
using DataX.Contract;
using DataX.Flow.CodegenRules;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.ConfigGeneration.Processor
{
    /// <summary>
    /// Produce the time window section
    /// </summary>
    [Shared]
    [Export(typeof(IFlowDeploymentProcessor))]
    public class ResolveTimeWindow : ProcessorBase
    {
        public const string TokenName_TimeWindows = "processTimeWindows";
        
        public override async Task<string> Process(FlowDeploymentSession flowToDeploy)
        {         
            var config = flowToDeploy.Config;
            var guiConfig = config?.GetGuiConfig();
            if (guiConfig == null)
            {
                return "no gui input, skipped.";
            }
            var rulesCode = flowToDeploy.GetAttachment<RulesCode>(PrepareTransformFile.AttachmentName_CodeGenObject);
            Ensure.NotNull(rulesCode, "rulesCode");
            Ensure.NotNull(rulesCode.MetricsRoot, "rulesCode.MetricsRoot");
            Ensure.NotNull(rulesCode.MetricsRoot.metrics, "rulesCode.MetricsRoot.metrics");

            var timewindows =  rulesCode.TimeWindows.Select(t =>
            {                
                return new TimeWindowSpec()
                {
                    Name = t.Key,
                    WindowDuration = t.Value
                };
            }).ToArray();

            flowToDeploy.SetObjectToken(TokenName_TimeWindows, timewindows);

            await Task.Yield();

            return "done";
        }
    }
}
