// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Contract;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.PublicService
{
    [Shared]
    [Export]
    public class JobOperation
    {
        /// <summary>
        /// Initialize an instance of <see cref="JobOperation"/>
        /// </summary>
        [ImportingConstructor]
        public JobOperation([Import] SparkJobOperation jobOpers, [Import] SparkJobData jobData)
        {
            this.SparkJobOperation = jobOpers;
            this.SparkJobData = jobData;
        }

        private SparkJobOperation SparkJobOperation { get; }
        private SparkJobData SparkJobData { get; }

        private static readonly TimeSpan _DefaultRetryTimeout = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan _DefaultRetryInterval = TimeSpan.FromMilliseconds(300);

        public async Task<Result> StartJob(string jobName)
        {
            return await this.SparkJobOperation.StartJob(jobName);
        }

        public async Task<Result> StopJob(string jobName)
        {
            return await this.SparkJobOperation.StopJob(jobName);
        }

        public async Task<Result> RestartJob(string jobName)
        {
            var timeout = DateTime.Now.Add(_DefaultRetryTimeout);
            return await this.SparkJobOperation.RestartJobWithRetries(jobName, timeout, _DefaultRetryInterval);
        }

        public async Task<Result[]> RestartAllJobs(string[] jobNames = null)
        {
            string[] actualJobNames = jobNames;
            if(actualJobNames == null || !actualJobNames.Any())
            {
                var jobs = await SparkJobData.GetAll();
                actualJobNames = jobs.Select(j => j.Name).ToArray();
            }    

            var results = new List<Result>();
            foreach (var jobName in actualJobNames)
            {
                var result = await RestartJob(jobName);
                results.Add(result);     
            }

            return results.ToArray<Result>();
        }

        protected static async Task<SparkJobFrontEnd[]> ConvertToFrontEnd(Task<SparkJobConfig[]> tasks)
        {
            var jobs = await tasks;
            return jobs.Select(SparkJobFrontEnd.FromSparkJobConfig).ToArray();
        }

        public Task<SparkJobFrontEnd[]> GetJobs()
        {
            return ConvertToFrontEnd(SparkJobData.GetAll());
        }

        public Task<SparkJobFrontEnd[]> GetJobsByNames(string[] names)
        {
            return ConvertToFrontEnd(SparkJobData.GetByNames(names));
        }

        public async Task<SparkJobFrontEnd> SyncJobState(string jobName)
        {
            return SparkJobFrontEnd.FromSparkJobConfig(await SparkJobOperation.SyncJobState(jobName));
        }

        public Task<SparkJobFrontEnd[]> SyncJobStateByNames(string[] names)
        {
            return ConvertToFrontEnd(SparkJobOperation.SyncJobStateByNames(names));
        }

        public Task<SparkJobFrontEnd[]> SyncAllJobState()
        {
            return ConvertToFrontEnd(SparkJobOperation.SyncAllJobState());
        }

        public async Task<Result> DeleteJob(string jobName)
        {
            return await this.SparkJobData.DeleteByName(jobName);
        }
    }
}
