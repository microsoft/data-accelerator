// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.ConfigGeneration.Processor
{
    /// <summary>
    /// Produce the timestamp column and watermark token
    /// </summary>
    [Shared]
    [Export(typeof(IFlowDeploymentProcessor))]
    public class ResolveTimestampColumn: ProcessorBase
    {
        public const string TokenName_ProcessTimestampColumn = "processTimestampColumn";
        public const string TokenName_ProcessWatermark= "processWatermark";
        
        public override async Task<string> Process(FlowDeploymentSession flowToDeploy)
        {
            var config = flowToDeploy.Config;
            var guiConfig = config.GetGuiConfig();

            if (guiConfig == null)
            {
                return "no gui input, skipped.";
            }

            flowToDeploy.SetStringToken(TokenName_ProcessTimestampColumn, guiConfig.Process.TimestampColumn);
            flowToDeploy.SetStringToken(TokenName_ProcessWatermark, guiConfig.Process.Watermark);

            await Task.Yield();
            return "done";
        }
    }
}
