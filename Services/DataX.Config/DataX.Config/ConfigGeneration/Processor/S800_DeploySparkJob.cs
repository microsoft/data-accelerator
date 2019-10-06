// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Config.ConfigGeneration.Processor;
using DataX.Config.Templating;
using DataX.Contract;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.ConfigGeneration.Processor
{
    /// <summary>
    /// Create or update the spark job
    /// </summary>
    [Shared]
    [Export(typeof(IFlowDeploymentProcessor))]
    public class DeploySparkJob : ProcessorBase
    {
        public const string AttachmentName_SparkJobNames = "sparkJobNames";

        [ImportingConstructor]
        public DeploySparkJob(SparkJobTemplateManager sparkJobTemplates, SparkJobData sparkJobs)
        {
            this.SparkJobData = sparkJobs;
            this.SparkJobTemplateData = sparkJobTemplates;
        }

        private SparkJobTemplateManager SparkJobTemplateData { get; }

        private SparkJobData SparkJobData { get; }

        public override int GetOrder()
        {
            return 800;
        }

        public override async Task<string> Process(FlowDeploymentSession flowToDeploy)
        {
            // Get spark job config template with the reference from flow config
            var jobConfigTemplateName = flowToDeploy.Config?.GetSparkJobConfigTemplateName();
            var defaultSparkJobConfig = await this.SparkJobTemplateData.GetByName(jobConfigTemplateName);
            Ensure.NotNull(defaultSparkJobConfig, "defaultSparkJobConfig");

            var sparkJobUpsertions = flowToDeploy.GetJobs()?.Select(job => DeploySingleJob(job, defaultSparkJobConfig));

            if (sparkJobUpsertions == null)
            {
                return "no jobs are defined, skipped";
            }

            // Ensure all creations are done successfully
            var upsertedJobNames = await Task.WhenAll(sparkJobUpsertions);
            var jobNames = ParseJobNames(upsertedJobNames);
            flowToDeploy.SetAttachment(AttachmentName_SparkJobNames, jobNames);

            return "done";
        }

        /// <summary>
        /// Delete the job config
        /// </summary>
        /// <param name="flowToDelete"></param>
        /// <returns></returns>
        public override async Task<string> Delete(FlowDeploymentSession flowToDelete)
        {
            var jobNames = flowToDelete?.Config?.JobNames;
            if (jobNames == null)
            {
                return "skipped";
            }

            foreach (var jobName in jobNames)
            {
                await SparkJobData.DeleteByName(jobName);
            }

            return "done";
        }

        /// <summary>
        /// create or update the spark job entry for the given job in deploying
        /// </summary>
        /// <returns>name of the spark job deployed</returns>
        private async Task<string[]> DeploySingleJob(JobDeploymentSession job, JsonConfig defaultSparkJobConfig)
        {
            List<string> names = new List<string>();
            foreach (var jc in job.JobConfigs)
            {
                // For each job
                //      replace config with tokens from job and flow, and the runtime config file path
                //      create spark job entry
                job.SparkJobName = jc.SparkJobName;
                job.SparkJobConfigFilePath = jc.SparkFilePath;
                var json = job.Tokens.Resolve(defaultSparkJobConfig);
                var newJob = SparkJobConfig.From(json);
                var jobName = newJob.Name;
                var existingJob = await SparkJobData.GetByName(jobName);

                if (existingJob != null)
                {
                    //keep the state of the old job so we can stop that 
                    newJob.SyncResult = existingJob.SyncResult;
                }

                var result = await this.SparkJobData.UpsertByName(jobName, newJob);
                if (!result.IsSuccess)
                {
                    throw new ConfigGenerationException($"Failed to upsert into SparkJob table for job '{jobName}': {result.Message}");
                }

                names.Add(jobName);
            }

            return names.ToArray();
        }

        /// <summary>
        /// parse job names
        /// </summary>
        /// <param name="jobNames"></param>
        /// <returns></returns>
        private string[] ParseJobNames(string[][] jobNames)
        {
            List<string> names = new List<string>();

            for (int i = 0; i < jobNames.Length; i++)
            {
                names.AddRange(jobNames[i].ToArray());
            }

            return names.ToArray();
        }

    }
}
