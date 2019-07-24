// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using DataX.Config.ConfigDataModel.RuntimeConfig;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Threading.Tasks;
using DataX.Config.ConfigDataModel;

namespace DataX.Config.ConfigGeneration.Processor
{
    /// <summary>
    /// Produce the spark job parameters
    /// </summary>
    [Shared]
    [Export(typeof(IFlowDeploymentProcessor))]
    public class ResolveSparkJobParams: ProcessorBase
    {
        public const string TokenName_SparkJobNumExecutors = "guiSparkJobNumExecutors";
        public const string TokenName_SparkJobJobExecutorMemory = "guiSparkJobExecutorMemory";
        public const string TokenName_SparkJobDatabricksMinWorkers = "guiSparkJobDatabricksMinWorkers";
        public const string TokenName_SparkJobDatabricksMaxWorkers = "guiSparkJobDatabricksMaxWorkers";
        public const string TokenName_DatabricksToken = "guiSparkDatabricksToken";
        public const string TokenName_SparkJobDatabricksAutoScale = "guiSparkJobDatabricksAutoScale";

        [ImportingConstructor]
        public ResolveSparkJobParams(ConfigGenConfiguration conf)
        {
            Configuration = conf;
        }
        private ConfigGenConfiguration Configuration { get; }
        public override async Task<string> Process(FlowDeploymentSession flowToDeploy)
        {
            var guiConfig = flowToDeploy.Config?.GetGuiConfig();
            if (guiConfig == null)
                
            {
                // If guiConfig is empty, get the number of executors from job common token and convert it to integer
                var executorsString = flowToDeploy.Config?.CommonProcessor?.JobCommonTokens?.GetOrDefault("sparkJobNumExecutors", null);
                if (executorsString != null)
                {
                    if (!int.TryParse(executorsString, out int executorsInt))
                    {
                        throw new ConfigGenerationException($"Invalid value for process.jobconfig.jobNumExecutors:'{executorsString}'.");
                    }
                    flowToDeploy.SetObjectToken("sparkJobNumExecutorsInt", executorsInt);
                }
                return "no gui config, skipped";
            }

            // Setting TokenName_SparkJobNumExecutors
            var numExecutorsString = guiConfig?.Process?.JobConfig?.JobNumExecutors;
            if(!int.TryParse(numExecutorsString, out int numExecutors))
            {
                throw new ConfigGenerationException($"Invalid value for process.jobconfig.jobNumExecutors:'{numExecutorsString}'.");
            }
            
            flowToDeploy.SetObjectToken(TokenName_SparkJobNumExecutors, numExecutors);

            // Setting TokenName_SparkJobJobExecutorMemory
            var jobExecutorMemoryString = guiConfig?.Process?.JobConfig?.JobExecutorMemory;
            if (!int.TryParse(jobExecutorMemoryString, out int jobExecutorMemory))
            {
                throw new ConfigGenerationException($"Invalid value for process.jobconfig.jobExecutorMemory:'{jobExecutorMemoryString}'.");
            }

            flowToDeploy.SetStringToken(TokenName_SparkJobJobExecutorMemory, $"{jobExecutorMemory}m");
            var sparkType = Configuration.TryGet(Constants.ConfigSettingName_SparkType, out string value) ? value : null;
            if (sparkType == Config.ConfigDataModel.Constants.SparkTypeDataBricks)
            {
                // Setting TokenName_SparkJobDatabricksMinWorkers
                var jobDatabricksMinWorkersString = guiConfig?.Process?.JobConfig?.JobDatabricksMinWorkers;
                if (!int.TryParse(jobDatabricksMinWorkersString, out int jobDatabricksMinWorkers))
                {
                    throw new ConfigGenerationException($"Invalid value for process.jobconfig.jobDatabricksMinWorkers:'{jobDatabricksMinWorkersString}'.");
                }

                flowToDeploy.SetStringToken(TokenName_SparkJobDatabricksMinWorkers, $"{jobDatabricksMinWorkers}");

                // Setting TokenName_SparkJobDatabricksMaxWorkers
                var jobDatabricksMaxWorkersString = guiConfig?.Process?.JobConfig?.JobDatabricksMaxWorkers;
                if (!int.TryParse(jobDatabricksMaxWorkersString, out int jobDatabricksMaxWorkers))
                {
                    throw new ConfigGenerationException($"Invalid value for process.jobconfig.jobDatabricksMaxWorkers:'{jobDatabricksMaxWorkersString}'.");
                }

                flowToDeploy.SetStringToken(TokenName_SparkJobDatabricksMaxWorkers, $"{jobDatabricksMaxWorkers}");

                // Setting TokenName_DatabricksToken
                var jobDatabricksTokenString = guiConfig?.DatabricksToken;
                flowToDeploy.SetStringToken(TokenName_DatabricksToken, $"{jobDatabricksTokenString}");

                // Setting TokenName_SparkJobDatabricksAutoScale
                var jobDatabricksAutoScaleString = guiConfig?.Process?.JobConfig?.JobDatabricksAutoScale;
                flowToDeploy.SetStringToken(TokenName_SparkJobDatabricksAutoScale, $"{jobDatabricksAutoScaleString}");
            }
            await Task.Yield();
            return "done";
        }
    }
}
