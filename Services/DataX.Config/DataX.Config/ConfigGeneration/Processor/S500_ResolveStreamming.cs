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
    /// Produce the input streaming section
    /// </summary>
    [Shared]
    [Export(typeof(IFlowDeploymentProcessor))]
    public class ResolveStreaming : ProcessorBase
    {
        public const string TokenName_InputStreamingCheckpointDir = "inputStreamingCheckpointDir";
        public const string TokenName_InputStreamingInterval = "inputStreamingIntervalInSeconds";
        
        public override async Task<string> Process(FlowDeploymentSession flowToDeploy)
        {
            var config = flowToDeploy.Config;
            var guiConfig = config.GetGuiConfig();

            flowToDeploy.SetStringToken(TokenName_InputStreamingCheckpointDir, $"hdfs://mycluster/dataxdirect/{JobMetadata.TokenPlaceHolder_JobName}/streaming/checkpoints");

            var intervalInSeconds = guiConfig?.Input?.Properties?.WindowDuration;
            flowToDeploy.SetStringToken(TokenName_InputStreamingInterval, intervalInSeconds);

            await Task.Yield();

            return "done";
        }
    }
}
