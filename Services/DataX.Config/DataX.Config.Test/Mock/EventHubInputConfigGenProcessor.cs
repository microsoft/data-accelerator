// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigGeneration.Processor;
using DataX.Config.Templating;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.Test.Config.Processor
{
    [Shared]
    [Export(typeof(IFlowDeploymentProcessor))]
    public class EventHubInputConfigGenProcessor : ProcessorBase
    {
        public const string TokenName_InputEventHubConnectionString = "inputEventHubConnectionString";
        public const string TokenName_InputEventHubConsumerGroup= "inputEventHubConsumerGroup";
        public const string TokenName_InputEventHubCheckpointDir = "inputEventHubCheckpointDir";
        public const string TokenName_InputEventHubCheckpointInterval = "inputEventHubCheckpointInterval";
        public const string TokenName_InputEventHubMaxRate = "inputEventHubMaxRate";
        public const string TokenName_InputEventHubFlushExistingCheckpoints = "inputEventHubFlushExistingCheckpoints";

        [ImportingConstructor]
        public EventHubInputConfigGenProcessor(IKeyVaultClient kvClient)
        {
            KeyVaultClient = kvClient;
        }

        private IKeyVaultClient KeyVaultClient { get; }
        
        public override async Task<string> Process(FlowDeploymentSession flowToDeploy)
        {
            //TODO: create eventhub consumer group
            var flowConfig = flowToDeploy.Config;
            var config = flowConfig?.GetGuiConfig()?.Input;

            //TODO: take care of other input types
            if(config!=null && "iothub".Equals(config.InputType, StringComparison.InvariantCultureIgnoreCase))
            {
                var props = config.Properties;
                if (props != null)
                {
                    var connectionString = props.InputEventhubConnection;
                    flowToDeploy.SetStringToken(TokenName_InputEventHubConnectionString, connectionString);

                    // TODO: figure out the right value
                    flowToDeploy.SetStringToken(TokenName_InputEventHubConsumerGroup, flowConfig.Name);
                    flowToDeploy.SetStringToken(TokenName_InputEventHubCheckpointDir, $"hdfs://mycluster/dataxdirect/{JobMetadata.TokenPlaceHolder_JobName}/eventhub/checkpoints");

                    var intervalInSeconds = props?.WindowDuration;
                    flowToDeploy.SetStringToken(TokenName_InputEventHubCheckpointInterval, intervalInSeconds);
                    flowToDeploy.SetObjectToken(TokenName_InputEventHubMaxRate, props.MaxRate);
                    flowToDeploy.SetObjectToken(TokenName_InputEventHubFlushExistingCheckpoints, true);
                }
            }
            await Task.Yield();

            return "done";
        }
    }
}
