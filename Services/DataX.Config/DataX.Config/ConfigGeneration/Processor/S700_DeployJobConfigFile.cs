// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Config.ConfigGeneration.Processor;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.ConfigGeneration.Processor
{
    /// <summary>
    /// Generate the config file to runtime storage
    /// </summary>
    [Shared]
    [Export(typeof(IFlowDeploymentProcessor))]
    public class DeployJobConfigFile : ProcessorBase
    {
        public const string ParameterObjectName_DefaultJobConfig = "defaultJobConfig";

        [ImportingConstructor]
        public DeployJobConfigFile(JobDataManager jobs, ConfigFlattenerManager flatteners)
        {
            this.JobData = jobs;
        }

        private JobDataManager JobData { get; }

        public override int GetOrder()
        {
            return 700;
        }

        public override async Task<string> Process(FlowDeploymentSession flowToDeploy)
        {
            // Deploy job configs
            var jobs = flowToDeploy.GetJobs();
            var deploymentTasks = jobs?.Select(async job => {
                var content = job.GetTokenString(GenerateJobConfig.TokenName_JobConfigContent);
                var filePath = job.GetTokenString(GenerateJobConfig.TokenName_JobConfigFilePath);
                if (content != null && filePath != null)
                {
                    job.SparkJobConfigFilePath = await this.JobData.SaveFile(filePath, content);
                }
                else
                {
                    job.SparkJobConfigFilePath = null;
                }

                return job.SparkJobConfigFilePath;
            });

            // Ensure all jobs configs are written successfully
            if (deploymentTasks == null)
            {
                return "no jobs, skipped";
            }

            await Task.WhenAll(deploymentTasks);
            
            return "done";
        }
    }
}
