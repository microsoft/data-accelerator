// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Config.Utility;
using DataX.Contract;
using System.Composition;
using System.Threading.Tasks;

namespace DataX.Config.ConfigGeneration.Processor
{
    /// <summary>
    /// Produce job config content and file path
    /// </summary>
    [Shared]
    [Export(typeof(IFlowDeploymentProcessor))]
    public class GenerateJobConfig : ProcessorBase
    {
        public const string ParameterObjectName_DefaultJobConfig = "defaultJobConfig";
        public const string TokenName_JobConfigContent = "jobConfigContent";

        [ImportingConstructor]
        public GenerateJobConfig(JobDataManager jobs)
        {
            this.JobData = jobs;
        }

        private JobDataManager JobData { get; }

        public override int GetOrder()
        {
            return 600;
        }

        public override async Task<string> Process(FlowDeploymentSession flowToDeploy)
        {
            var flowConfig = flowToDeploy.Config;

            if (flowConfig.GetGuiConfig()?.Input?.Mode == Constants.InputMode_Batching)
            {
                return "done";
            }

            // set the default job config
            var defaultJobConfig = JsonConfig.From(flowConfig.CommonProcessor?.Template);
            Ensure.NotNull(defaultJobConfig, "defaultJobConfig");

            // Deploy job configs
            var jobsToDeploy = flowToDeploy?.GetJobs();
            if (jobsToDeploy != null)
            {
                foreach (var job in jobsToDeploy)
                {
                    GenerateJobConfigContent(job, job.GetTokenString(PrepareJobConfigVariables.TokenName_RuntimeConfigFolder), defaultJobConfig);
                }

                return "done";
            }
            else
            {
                await Task.Yield();
                return "no jobs, skipped";
            }
        }

        private void GenerateJobConfigContent(JobDeploymentSession job, string destFolder, JsonConfig defaultJobConfig)
        {
            Ensure.NotNull(destFolder, "destFolder");
            Ensure.NotNull(defaultJobConfig, "defaultJobConfig");

            // replace config with tokens from job and the flow

            var newJobConfig = job.Tokens.Resolve(defaultJobConfig);
            Ensure.NotNull(newJobConfig, "newJobConfig");
            job.SetJsonToken(TokenName_JobConfigContent, newJobConfig);

            var jc = new JobConfig
            {
                Content = newJobConfig.ToString(),
                FilePath = ResourcePathUtil.Combine(destFolder, job.Name + ".conf"),
                SparkJobName = job.SparkJobName,
            };

            job.JobConfigs.Add(jc);
        }
        /// <summary>
        /// Delete the runtime job configs
        /// </summary>
        /// <param name="flowToDelete"></param>
        /// <returns></returns>
        public override async Task<string> Delete(FlowDeploymentSession flowToDelete)
        {
            var flowConfig = flowToDelete.Config;
            var runtimeConfigsFolder = flowConfig.GetJobConfigDestinationFolder();

            flowToDelete.SetStringToken(PrepareJobConfigVariables.TokenName_RuntimeConfigFolder, runtimeConfigsFolder);
            var folderToDelete = flowToDelete.GetTokenString(PrepareJobConfigVariables.TokenName_RuntimeConfigFolder);
            return await this.JobData.DeleteConfigs(folderToDelete);
        }
    }
}
