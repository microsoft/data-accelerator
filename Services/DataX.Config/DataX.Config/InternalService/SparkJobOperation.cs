// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Contract;
using DataX.Contract.Exception;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace DataX.Config
{
    [Shared]
    [Export]
    public class SparkJobOperation
    {
        [ImportingConstructor]
        public SparkJobOperation(SparkJobData jobData, SparkClusterManager clusterManager, ILogger<SparkJobOperation> logger)
        {
            this.JobData= jobData;
            this.ClusterManager = clusterManager;
            Logger = logger;
        }

        //Livy client does not garantee success of an API call.  This will be the default number of tries.  Livy client expires each request after 2 minutes, so ~1hour
        private const int _DefaultRetries = 30;
        private const int _DefaultIntervalInMs = 1000;

        private SparkJobData JobData{ get; }
        private SparkClusterManager ClusterManager { get; }
        private ILogger Logger { get; }

        /// <summary>
        /// Job management to start a job by name
        /// </summary>
        /// <param name="jobName">internal job name</param>
        /// <returns></returns>
        public async Task<Result> StartJob(string jobName)
        {
            Ensure.NotNull(jobName, "jobName");
            Logger.LogInformation($"starting job '{jobName}'");
            var job = await EnsureJobState(jobName);
            var sparkJobClient = await ClusterManager.GetSparkJobClient(job.Cluster, job.DatabricksToken);
            var result = await sparkJobClient.SubmitJob(job.Options);

            // Update job state
            job.SyncResult = result;
            return await this.JobData.UpdateSyncResultByName(jobName, result);
        }

        /// <summary>
        /// Job management to start a job by name with retries
        /// </summary>
        /// <param name="jobName">internal job name</param>
        /// <param name="timeout">time to stop retrying and throw exception if it keep failing</param>
        /// <param name="interval">interval between retries</param>
        /// <returns></returns>
        public Task<Result> StartJobWithRetries(string jobName, DateTime timeout, TimeSpan interval)
        {
            return RetryInCaseOfException(() => StartJob(jobName), timeout, interval);
        }

        /// <summary>
        /// Job Management to stop a job by name if it is not stopped yet
        /// </summary>
        /// <param name="jobName">job name</param>
        /// <returns></returns>
        public async Task<Result> StopJob(string jobName)
        {
            Ensure.NotNull(jobName, "jobName");
            Logger.LogInformation($"stoping job '{jobName}'");

            var job = await SyncJobState(jobName);
            var jobState = job.SyncResult?.JobState;

            switch (jobState)
            {
                case JobState.Starting:
                case JobState.Running:
                    var sparkJobClient = await ClusterManager.GetSparkJobClient(job.Cluster, job.DatabricksToken);
                    var result = await sparkJobClient.StopJob(job.SyncResult.ClientCache);
                    job.SyncResult = result;
                    return await this.JobData.UpdateSyncResultByName(jobName, result);
                case JobState.Success:
                case JobState.Idle:
                    return new SuccessResult($"job '{jobName}' has already been stopped");
                case JobState.Error:
                    return new SuccessResult($"job '{jobName}' is currently in an error state");
                default:
                    throw new GeneralException($"Unexpected state '{jobState}' for job '{jobName}'");
            }
        }

        /// <summary>
        /// Job management to stop a job by name with retries
        /// </summary>
        /// <param name="jobName">internal job name</param>
        /// <param name="timeout">time to stop retrying and throw exception if it keep failing</param>
        /// <param name="interval">interval between retries</param>
        /// <returns></returns>
        public Task<Result> StopJobWithRetries(string jobName, DateTime timeout, TimeSpan interval)
        {
            return RetryInCaseOfException(() => StopJob(jobName), timeout, interval);
        }

        /// <summary>
        /// Job Management to restart a job by name
        /// </summary>
        /// <param name="jobName">internal name</param>
        /// <returns></returns>
        public async Task<Result> RestartJob(string jobName)
        {
            var result = await StopJob(jobName);
            if (!result.IsSuccess)
            {
                return result;
            }

            result = await StartJob(jobName);
            return result;
        }

        /// <summary>
        /// Job management to restart a job by name with retries
        /// </summary>
        /// <param name="jobName">internal job name</param>
        /// <param name="timeout">time to stop retrying and throw exception if it keep failing</param>
        /// <param name="interval">interval between retries</param>
        /// <returns></returns>
        public async Task<Result> RestartJobWithRetries(string jobName, DateTime timeout, TimeSpan interval)
        {
            var result = await StopJobWithRetries(jobName, timeout, interval);
            if (!result.IsSuccess)
            {
                return result;
            }

            result = await StartJobWithRetries(jobName, timeout, interval);
            return result;
        }

        /// <summary>
        /// Get job details for a specific job id from Job manager
        /// </summary>
        /// <param name="jobName">jobName</param>
        /// <returns></returns>
        public async Task<SparkJobConfig> SyncJobState(string jobName)
        {
            Ensure.NotNull(jobName, "jobName");
            Logger.LogInformation($"sync'ing job '{jobName}'");

            // Get the job config data from internal data and ensure job is in Idle state
            var job = await JobData.GetByName(jobName);
            Ensure.NotNull(job, "job");
            
            var state = job.SyncResult;
            if (state == null)
            {
                state = new SparkJobSyncResult();
            }
            
            if (state.ClientCache != null && state.ClientCache.Type != JTokenType.Null)
            {
                var sparkJobClient = await ClusterManager.GetSparkJobClient(job.Cluster, job.DatabricksToken);
                var newResult = await sparkJobClient.GetJobInfo(state.ClientCache);
                state = newResult;
                Logger.LogInformation($"got state for job '{jobName}'");
            }
            else
            {
                state.JobState = JobState.Idle;
                Logger.LogInformation($"no client cache for job '{jobName}', set state to '{state.JobState}'");
            }

            if (!state.Equals(job.SyncResult))
            {
                var updateResult = await JobData.UpdateSyncResultByName(jobName, state);
                if (!updateResult.IsSuccess)
                {
                    throw new GeneralException($"Failed to update job client cache for name '{jobName}': returned message '{updateResult.Message}'");
                }

                job.SyncResult = state;
                Logger.LogInformation($"done sync'ing the state for job '{jobName}', state updated");
            }
            else
            {
                Logger.LogInformation($"done sync'ing the state for job '{jobName}', no changes");
            }

            return job;
        }
        

        /// <summary>
        /// Sync specific names
        /// </summary>
        /// <param name="names">array of names</param>
        /// <returns></returns>
        public async Task<SparkJobConfig[]> SyncJobStateByNames(string[] names)
        {
            Ensure.NotNull(names, "names");
            Logger.LogInformation($"sync'ing job states for jobs '{names}'");
            return await Task.WhenAll(names.Select(name => SyncJobState(name)));
        }

        /// <summary>
        /// Get all job information from Livy service
        /// </summary>
        /// <returns></returns>
        public async Task<SparkJobConfig[]> SyncAllJobState()
        {
            var jobs = await JobData.GetAll();
            Logger.LogInformation($"sync'ing job states for all jobs'");
            return await SyncJobStateByNames(jobs.Select(job => job.Name).ToArray());
        }

        /// <summary>
        /// Make sure a job is in the specified state within 
        /// </summary>
        /// <param name="jobName">job name</param>
        /// <param name="retries">retries to check for state</param>
        /// <param name="intervalInMs">interval in milliseconds to start perform next check</param>
        /// <returns>the job config</returns>
        private async Task<SparkJobConfig> EnsureJobState(string jobName, int retries = _DefaultRetries, int intervalInMs = _DefaultIntervalInMs)
        {
            Ensure.NotNull(jobName, "jobName");
            Logger.LogInformation($"ensuring job '{jobName}' state to be idle or success");
            int i = 0;
            while (i < retries)
            {
                try
                {
                    //Get Job data
                    var job = await SyncJobState(jobName);
                    if (VerifyJobStopped(job.SyncResult.JobState))
                    {
                        return job;
                    }
                }
                catch(Exception e)
                {
                    Logger.LogError(e, $"EnsureJobState encounter exception:'{e}', this is attempt#{i} out of total retries:{retries}");
                }

                //wait some time and retry if state is not as expected
                await Task.Delay(intervalInMs);
                i++;
            }

            throw new GeneralException($"Job '{jobName}' failed to get to state idle or success after '{retries}' sync attempts");
        }

        /// <summary>
        /// Verify the job is not in running state
        /// </summary>
        /// <param name="jobState">job state</param>
        /// <returns>true if job is not running, false otherwise</returns>
        public static bool VerifyJobStopped(JobState jobState)
        {
            return (jobState == JobState.Idle || jobState == JobState.Success || jobState == JobState.Error);
        }

        public async Task<T> RetryInCaseOfException<T>(Func<Task<T>> func, DateTime timeout, TimeSpan interval)
        {
            Exception innerException = null;

            while(DateTime.Now <= timeout)
            {
                try
                {
                    return await func();
                }
                catch (Exception e)
                {
                    innerException = e;
                    Logger.LogWarning($"Encounter exception '{e}' in executing '{func}', wait for next retry before time '{timeout}'");
                }

                await Task.Delay(interval);
            }

            throw new GeneralException($"failed executing '{func}' for '{innerException}'", innerException);
        }
    }
}
