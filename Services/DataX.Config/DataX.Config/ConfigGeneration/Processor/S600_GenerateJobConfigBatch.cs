// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Config.ConfigDataModel.RuntimeConfig;
using DataX.Config.Utility;
using DataX.Contract;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataX.Config.ConfigGeneration.Processor
{
    /// <summary>
    /// Produce job config content and file path
    /// </summary>
    [Shared]
    [Export(typeof(IFlowDeploymentProcessor))]
    public class GenerateJobConfigBatch : ProcessorBase
    {
        public const string TokenName__DefaultJobConfig = "defaultJobConfig";
        public const string TokenName_InputBatching = "inputBatching";

        [ImportingConstructor]
        public GenerateJobConfigBatch(JobDataManager jobs, FlowDataManager flowData, IKeyVaultClient keyVaultClient)
        {
            this.JobData = jobs;
            this.FlowData = flowData;
            this.KeyVaultClient = keyVaultClient;
        }

        private JobDataManager JobData { get; }
        private FlowDataManager FlowData { get; }
        private IKeyVaultClient KeyVaultClient { get; }

        public override int GetOrder()
        {
            return 600;
        }

        public override async Task<string> Process(FlowDeploymentSession flowToDeploy)
        {
            var flowConfig = flowToDeploy.Config;

            if (flowConfig.GetGuiConfig()?.Input?.Mode != Constants.InputMode_Batching)
            {
                return "done";
            }

            // set the default job config
            var defaultJobConfig = JsonConfig.From(flowConfig.CommonProcessor?.Template);
            Ensure.NotNull(defaultJobConfig, "defaultJobConfig");
            flowToDeploy.SetAttachment(TokenName__DefaultJobConfig, defaultJobConfig);

            // Deploy job configs
            var jobsToDeploy = flowToDeploy?.GetJobs();
            if (jobsToDeploy != null)
            {
                foreach (var job in jobsToDeploy)
                {
                    await GenerateJobConfigContent(job, job.GetTokenString(PrepareJobConfigVariables.TokenName_RuntimeConfigFolder), defaultJobConfig).ConfigureAwait(false);
                }

                return "done";
            }
            else
            {
                await Task.Yield();
                return "no jobs, skipped";
            }
        }

        /// <summary>
        /// Generate job config with the jobs scheduled
        /// </summary>
        /// <returns></returns>
        private async Task GenerateJobConfigContent(JobDeploymentSession job, string destFolder, JsonConfig defaultJobConfig)
        {
            Ensure.NotNull(destFolder, "destFolder");
            Ensure.NotNull(defaultJobConfig, "defaultJobConfig");

            var inputConfig = job.Flow.Config?.GetGuiConfig();
            if (inputConfig != null)
            {
                var jcons = new List<JobConfig>();

                for (int i = 0; i < inputConfig.BatchList.Length; i++)
                {
                    job.JobConfigs.AddRange(await GetJobConfig(job, inputConfig.BatchList[i], destFolder, defaultJobConfig, i).ConfigureAwait(false));
                }
            }
        }

        /// <summary>
        /// Prepare for scheduling batch jobs
        /// e.g. Calculate the processTime and startTime and endTime
        /// </summary>
        /// <returns></returns>
        private async Task<List<JobConfig>> GetJobConfig(JobDeploymentSession job, FlowGuiInputBatchJob batchingJob,
          string destFolder,
          JsonConfig defaultJobConfig,
          int index)
        {
            bool isOneTime = batchingJob.Type == Constants.Batch_OneTime;
            var jQueue = new List<JobConfig>();
            var batchProps = batchingJob.Properties;

            if (!ConfigHelper.ShouldScheduleJob(batchingJob.Disabled, isOneTime, batchProps.StartTime, batchProps.EndTime))
            {
                return jQueue;
            }

            var configStartTime = (DateTime)batchProps.StartTime;
            var configEndTime = batchProps.EndTime;

            var interval = ConfigHelper.TranslateInterval(batchProps.Interval, batchProps.IntervalType);
            var delay = ConfigHelper.TranslateDelay(batchProps.Delay, batchProps.DelayType);
            var window = ConfigHelper.TranslateWindow(batchProps.Window, batchProps.WindowType);

            var currentTime = DateTime.UtcNow;

            if (!isOneTime)
            {

                if (!ConfigHelper.IsValidRecurringJob(currentTime, configStartTime, configEndTime))
                {
                    await DisableBatchConfig(job.Flow.Config, index).ConfigureAwait(false);
                    return jQueue;
                }
            }

            DateTime startTime;
            DateTime? endTime;

            var prefix = "";

            if (!isOneTime)
            {
                if (string.IsNullOrEmpty(batchProps.LastProcessedTime))
                {
                    startTime = configStartTime;
                    endTime = currentTime;
                }
                else
                {
                    var lastProcessedTimeFromConfig = UnixTimestampToDateTime(batchProps.LastProcessedTime);
                    var startTimeBasedOnLastProcessedTImeFromConfig = lastProcessedTimeFromConfig.Add(interval);

                    startTime = startTimeBasedOnLastProcessedTImeFromConfig;
                    endTime = currentTime;
                }
            }
            else
            {
                prefix = "-OneTime";

                startTime = configStartTime;
                endTime = configEndTime;
            }

            DateTime lastProcessingTime = new DateTime();
            for (var processingTime = startTime; processingTime <= endTime; processingTime += interval)
            {
                lastProcessingTime = processingTime;
                var processingTimeBasedOnInterval = ConfigHelper.NormalizeTimeBasedOnInterval(processingTime, batchProps.IntervalType, new TimeSpan());
                var processingTimeBasedOnDelay = ConfigHelper.NormalizeTimeBasedOnInterval(processingTime, batchProps.IntervalType, delay);
                JobConfig jc = ScheduleSingleJob(job, destFolder, defaultJobConfig, isOneTime, interval, window, processingTimeBasedOnDelay, processingTimeBasedOnInterval, prefix);
                jQueue.Add(jc);
            }

            if (!isOneTime)
            {
                if (lastProcessingTime != DateTime.MinValue)
                {
                    var uTimestamp = DateTimeToUnixTimestamp(lastProcessingTime);
                    if (uTimestamp > 0)
                    {
                        var ret = await UpdateLastProcessedTime(job.Flow.Config, index, uTimestamp).ConfigureAwait(false);
                        if (!ret.IsSuccess)
                        {
                            throw new ConfigGenerationException(ret.Message);
                        }
                    }
                }
            }
            else
            {
                // OneTime
                var ret = await DisableBatchConfig(job.Flow.Config, index).ConfigureAwait(false);
                if (!ret.IsSuccess)
                {
                    throw new ConfigGenerationException(ret.Message);
                }

            }

            return jQueue;
        }

        /// <summary>
        /// Schedule a batch job
        /// </summary>
        /// <returns></returns>
        private JobConfig ScheduleSingleJob(JobDeploymentSession job, string destFolder, JsonConfig defaultJobConfig, bool isOneTime, TimeSpan interval, TimeSpan window, DateTime processTime, DateTime scheduledTime, string prefix = "")
        {
            var ps_s = processTime;
            var ps_e = processTime.Add(interval).AddMilliseconds(-1); //ENDTIME

            var pe_s = ps_s.Add(-window);
            var pe_e = ps_e.Add(-window); // STARTTIME

            var dateString = ConvertDateToString(scheduledTime);
            var suffix = prefix + $"-{Regex.Replace(dateString, "[^0-9]", "")}";
            var jobName = job.Name + suffix;
            job.SetStringToken("name", jobName);

            Ensure.NotNull(defaultJobConfig, "defaultJobConfig");

            var processStartTime = ConvertDateToString(pe_e);
            var processEndTime = ConvertDateToString(ps_e);

            destFolder = GetJobConfigFilePath(isOneTime, dateString, destFolder);
            var jc = new JobConfig
            {
                Content = GetBatchConfigContent(job, defaultJobConfig.ToString(), processStartTime, processEndTime),
                FilePath = ResourcePathUtil.Combine(destFolder, job.Name + ".conf"),
                Name = jobName,
                SparkJobName = job.SparkJobName + suffix,
                ProcessStartTime = processStartTime,
                ProcessEndTime = processEndTime,
                ProcessingTime = dateString,
                IsOneTime = isOneTime
            };

            return jc;
        }

        /// <summary>
        /// Get the job config path based on the job type
        /// </summary>
        /// <returns></returns>
        private static string GetJobConfigFilePath(bool isOneTime, string partitionName, string baseFolder)
        {
            var oneTimeFolderName = "";
            if (isOneTime)
            {
                oneTimeFolderName = $"OneTime/{Regex.Replace(partitionName, "[^0-9]", "")}";
            }
            else
            {
                oneTimeFolderName = $"Recurring/{Regex.Replace(partitionName, "[^0-9]", "")}";
            }

            return ResourcePathUtil.Combine(baseFolder, oneTimeFolderName);
        }

        /// <summary>
        /// Get a batch job config
        /// </summary>
        /// <returns></returns>
        private static string GetBatchConfigContent(JobDeploymentSession job, string content, string processStartTime, string processEndTime)
        {
            var specsBackup = job.GetAttachment<InputBatchingSpec[]>(TokenName_InputBatching);

            foreach (var spec in specsBackup)
            {
                spec.ProcessStartTime = processStartTime;
                spec.ProcessEndTime = processEndTime;
            }

            job.SetObjectToken(TokenName_InputBatching, specsBackup);

            var jsonContent = job.Tokens.Resolve(content);

            return jsonContent;
        }

        public override async Task<string> Delete(FlowDeploymentSession flowToDelete)
        {
            var flowConfig = flowToDelete.Config;
            var runtimeConfigsFolder = flowConfig.GetJobConfigDestinationFolder();

            flowToDelete.SetStringToken(PrepareJobConfigVariables.TokenName_RuntimeConfigFolder, runtimeConfigsFolder);
            var folderToDelete = flowToDelete.GetTokenString(PrepareJobConfigVariables.TokenName_RuntimeConfigFolder);
            return await this.JobData.DeleteConfigs(folderToDelete);
        }

        /// <summary>
        /// Convert a datetime to string in an expected format
        /// Implement this specific helper function since using "o" doesn't work
        /// </summary>
        /// <returns></returns>
        private static string ConvertDateToString(DateTime dateTime)
        {
            return dateTime.ToString("s", CultureInfo.InvariantCulture) + "Z";
        }

        /// <summary>
        /// Convert a datetime to an epoch timestamp
        /// </summary>
        /// <returns></returns>
        public static long DateTimeToUnixTimestamp(DateTime dateTime)
        {
            DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            TimeSpan elapsedTime = dateTime - unixStart;
            return (long)elapsedTime.TotalSeconds;
        }

        /// <summary>
        /// Convert an epoch timestamp to a datetime
        /// </summary>
        /// <returns></returns>
        public static DateTime UnixTimestampToDateTime(string unixTime)
        {
            double uTime = Convert.ToDouble(unixTime, CultureInfo.InvariantCulture);
            DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            return unixStart.AddSeconds(uTime);
        }

        /// <summary>
        /// Disable a batch job in the config
        /// </summary>
        /// <returns></returns>
        private async Task<Result> DisableBatchConfig(FlowConfig config, int index)
        {
            var existingFlow = await FlowData.GetByName(config.Name).ConfigureAwait(false);
            Result result = null;
            if (existingFlow != null)
            {
                var gui = config.GetGuiConfig();
                if (gui != null)
                {
                    var batch = gui.BatchList[index];
                    batch.Disabled = true;

                    config.Gui = JObject.FromObject(gui);
                    result = await FlowData.UpdateGuiForFlow(config.Name, config.Gui).ConfigureAwait(false);
                }
            }

            return result;
        }

        /// <summary>
        /// Update the last processed time for a batch job in the config
        /// </summary>
        /// <returns></returns>
        private async Task<Result> UpdateLastProcessedTime(FlowConfig config, int index, long value)
        {
            var existingFlow = await FlowData.GetByName(config.Name).ConfigureAwait(false);
            Result result = null;
            if (existingFlow != null)
            {
                var gui = config.GetGuiConfig();
                if (gui != null)
                {
                    var batch = gui.BatchList[index];
                    batch.Properties.LastProcessedTime = value.ToString(CultureInfo.InvariantCulture);

                    config.Gui = JObject.FromObject(gui);
                    result = await FlowData.UpdateGuiForFlow(config.Name, config.Gui).ConfigureAwait(false);
                }
            }

            return result;
        }
    }
}
