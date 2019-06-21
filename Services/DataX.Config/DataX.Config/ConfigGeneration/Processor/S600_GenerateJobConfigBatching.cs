// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
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
    public class GenerateJobConfigBatching : ProcessorBase
    {
        public const string ParameterObjectName_DefaultJobConfig = "defaultJobConfig";

        [ImportingConstructor]
        public GenerateJobConfigBatching(JobDataManager jobs, FlowDataManager flowData)
        {
            this.JobData = jobs;
            this.FlowData = flowData;
        }

        private JobDataManager JobData { get; }
        private FlowDataManager FlowData { get; }

        public override int GetOrder()
        {
            return 600;
        }

        public override async Task<string> Process(FlowDeploymentSession flowToDeploy)
        {
            var flowConfig = flowToDeploy.Config;

            if (flowConfig.GetGuiConfig().Input.Mode == Constants.InputMode_Streaming)
            {
                return "done";
            }

            // set the default job config
            var defaultJobConfig = JsonConfig.From(flowConfig.CommonProcessor?.Template);
            Ensure.NotNull(defaultJobConfig, "defaultJobConfig");
            flowToDeploy.SetAttachment(ParameterObjectName_DefaultJobConfig, defaultJobConfig);

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

        private async Task GenerateJobConfigContent(JobDeploymentSession job, string destFolder, JsonConfig defaultJobConfig)
        {
            Ensure.NotNull(destFolder, "destFolder");
            Ensure.NotNull(defaultJobConfig, "defaultJobConfig");

            var inputConfg = job.Flow.Config?.GetGuiConfig();

            var jcons = new List<JobConfig>();


            job.JobConfigs.AddRange(await GetJobConfig(job, inputConfg.Input.Batching, inputConfg.Input.Batching.Recurring, destFolder, defaultJobConfig).ConfigureAwait(false));

            foreach (var item in inputConfg.Input.Batching.Onetime)
            {
                job.JobConfigs.AddRange(await GetJobConfig(job, inputConfg.Input.Batching, item, destFolder, defaultJobConfig, true).ConfigureAwait(false));
            }
        }

        private async Task<List<JobConfig>> GetJobConfig(JobDeploymentSession job, FlowGuiInputBatching flowGuiInputBatching, FlowGuiInputBatchingJob batchingJob,
          string destFolder,
          JsonConfig defaultJobConfig,
          bool isOneTime = false)
        {
            var jQueue = new List<JobConfig>();
            var schedulerStartTime = DateTime.Parse(batchingJob.StartTime);
            var schedulerEndTime = DateTime.Parse(batchingJob.EndTime);
            var interval = TranslateInterval(batchingJob.Interval);
            var delay = TranslateDelay(batchingJob.Offset);
            var window = TranslateWindow(batchingJob.Window);

            DateTime processedTime;

            if (batchingJob.Disabled)
            {
                return jQueue;
            }

            if (!isOneTime)
            {
                if (schedulerEndTime < DateTime.Now)
                {
                    DisableBatchConfig(job.Flow.Config, isOneTime).ConfigureAwait(false);
                    return jQueue;
                }

                // Recurring New
                if (string.IsNullOrEmpty(batchingJob.LastProcessedTime))
                {
                    var processingTime = schedulerStartTime;
                    var scheduledTime = NormalizeTimeBasedOnInterval(schedulerStartTime, batchingJob.Interval, delay);

                    JobConfig jc = ScheduleSingleJob(job, destFolder, defaultJobConfig, isOneTime, interval, window, scheduledTime, processingTime, out processedTime);
                    processedTime = processingTime;
                    jQueue.Add(jc);

                }
                else
                {
                    // Recurring Existing 

                    var lastProcessedTime = UnixTimestampToDateTime(batchingJob.LastProcessedTime);
                    var processTime = lastProcessedTime.AddMinutes(interval);

                    var processingTime = processTime;
                    var scheduledTime = NormalizeTimeBasedOnInterval(processTime, batchingJob.Interval, delay);

                    JobConfig jc = ScheduleSingleJob(job, destFolder, defaultJobConfig, isOneTime, interval, window, scheduledTime, processingTime, out processedTime);
                    processedTime = processingTime;
                    jQueue.Add(jc);
                }

                var uTimestamp = DateTimeToUnixTimestamp(processedTime);
                var ret = await this.FlowData.UpdateLastProcessedTimeForFlow(job.Name, uTimestamp).ConfigureAwait(false);
                if (!ret.IsSuccess)
                {
                    throw new ConfigGenerationException(ret.Message);
                }


            }
            else
            {
                // OneTime
                var processTime = schedulerStartTime;
                while (processTime <= schedulerEndTime)
                {
                    var processingTime = processTime;
                    var scheduledTime = NormalizeTimeBasedOnInterval(processTime, batchingJob.Interval, delay);
                    var prefix = "-OneTime";
                    JobConfig jc = ScheduleSingleJob(job, destFolder, defaultJobConfig, isOneTime, interval, window, processTime, processingTime, out processedTime, prefix);
                    processTime = processTime.AddMinutes(interval);
                    jQueue.Add(jc);
                }

                var ret = await DisableBatchConfig(job.Flow.Config, isOneTime).ConfigureAwait(false);
                if (!ret.IsSuccess)
                {
                    throw new ConfigGenerationException(ret.Message);
                }
            }
            return jQueue;
        }

        private static JobConfig ScheduleSingleJob(JobDeploymentSession job, string destFolder, JsonConfig defaultJobConfig, bool isOneTime, long interval, TimeSpan window, DateTime processTime, DateTime scheduledTime, out DateTime processedTime, string prefix = "")
        {
            var ps_s = processTime;
            var ps_e = processTime.AddMinutes(interval).AddMilliseconds(-1); //ENDTIME

            var pe_s = ps_s.Add(-window);
            var pe_e = ps_e.Add(-window); // STARTTIME

            var dateString = ConvertDateToString(scheduledTime);
            var suffix = prefix + $"-{Regex.Replace(dateString, "[^0-9]", "")}";
            var jobName = job.Name + suffix;
            job.SetStringToken("name", jobName);

            var newJobConfig = job.Tokens.Resolve(defaultJobConfig);
            Ensure.NotNull(newJobConfig, "newJobConfig");

            processedTime = ps_s;
            var jc = new JobConfig
            {
                Content = newJobConfig.ToString(),
                FilePath = ResourcePathUtil.Combine(destFolder, job.Name + ".json"),
                Name = jobName,
                SparkJobName = job.SparkJobName + suffix,
                ProcessStartTime = ConvertDateToString(pe_e),
                ProcessEndTime = ConvertDateToString(ps_e),
                ProcessingTime = dateString,
                IsOneTime = isOneTime
            };

            return jc;
        }
        
        public override async Task<string> Delete(FlowDeploymentSession flowToDelete)
        {
            var flowConfig = flowToDelete.Config;
            var runtimeConfigsFolder = flowConfig.GetJobConfigDestinationFolder();

            flowToDelete.SetStringToken(PrepareJobConfigVariables.TokenName_RuntimeConfigFolder, runtimeConfigsFolder);
            var folderToDelete = flowToDelete.GetTokenString(PrepareJobConfigVariables.TokenName_RuntimeConfigFolder);
            return await this.JobData.DeleteConfigs(folderToDelete);
        }
        
        private static DateTime NormalizeTimeBasedOnInterval(DateTime dateTime, string interval, TimeSpan delay)
        {
            dateTime = dateTime.Add(-delay);
            int second = dateTime.Second;
            int minute = dateTime.Minute;
            int hour = dateTime.Hour;
            int day = dateTime.Day;
            int month = dateTime.Month;
            int year = dateTime.Year;

            switch (interval)
            {
                case "m":
                    {
                        second = 0;
                        break;
                    }
                case "h":
                    {
                        minute = 0;
                        break;
                    }
                case "d":
                    {
                        minute = 0;
                        hour = 0;
                        break;
                    }
                case "mm":
                    {
                        minute = 0;
                        hour = 0;
                        day = 0;
                        break;
                    }
                default:
                    {
                        minute = 0;
                        hour = 0;
                        break;
                    }
            }

            return new DateTime(year, month, day, hour, minute, second);
        }

        private static int TranslateIntervalHelper(string value)
        {
            switch (value)
            {
                case "m":
                    return 1;
                case "h":
                    return 60;
                case "d":
                    return 60 * 24;
                case "mm":
                    return 60 * 24 * 30;
                default:
                    return 1;
            }
        }

        private static int TranslateInterval(string interval)
        {
            return TranslateIntervalHelper(interval);
        }

        private static TimeSpan TranslateWindow(string window)
        {
            if (string.IsNullOrEmpty(window))
            {
                return new TimeSpan();
            }

            var values = window.Split(' ');
            if (values == null || values.Length < 2)
            {
                throw new ConfigGenerationException($"Batching window value is not correct");
            }

            var translatedValue = TranslateIntervalHelper(values[1]);
            int multiplier = Convert.ToInt32(values[0], CultureInfo.InvariantCulture);
            translatedValue = translatedValue * multiplier;

            var timeSpan = new TimeSpan(0, 0, translatedValue - 1, 59, 59);
            return timeSpan;
        }

        private static TimeSpan TranslateDelay(string offset)
        {
            if (string.IsNullOrEmpty(offset))
            {
                return new TimeSpan();
            }
            var values = offset.Split(' ');
            if (values == null || values.Length < 2)
            {
                throw new ConfigGenerationException($"Batching offset value is not correct");
            }

            var translatedValue = TranslateIntervalHelper(values[1]);
            int multiplier = Convert.ToInt32(values[0], CultureInfo.InvariantCulture);
            translatedValue = translatedValue * multiplier;

            var timeSpan = new TimeSpan(0, 0, translatedValue, 0, 0);
            return timeSpan;
        }

        private static string ConvertDateToString(DateTime dateTime)
        {
            return dateTime.ToString("s", CultureInfo.InvariantCulture) + "Z";
        }

        public static long DateTimeToUnixTimestamp(DateTime dateTime)
        {
            DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            TimeSpan elapsedTime = dateTime - unixStart;
            return (long)elapsedTime.TotalSeconds;
        }

        public static DateTime UnixTimestampToDateTime(string unixTime)
        {
            double uTime = Convert.ToDouble(unixTime, CultureInfo.InvariantCulture);
            DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            return unixStart.AddSeconds(uTime);
        }
        
        private async Task<Result> DisableBatchConfig(FlowConfig config, bool isOneTime)
        {
            var existingFlow = await FlowData.GetByName(config.Name).ConfigureAwait(false);
            Result result = null;
            if (existingFlow != null)
            {
                var gui = config.GetGuiConfig();

                if (isOneTime)
                {
                    var jobs = gui.Input.Batching.Onetime;

                    foreach (var job in jobs)
                    {
                        job.Disabled = true;
                    }
                }
                else
                {
                    var job = gui.Input.Batching.Recurring;

                    job.Disabled = true;
                }

                config.Gui = JObject.FromObject(gui);
                result = await FlowData.UpdateGuiForFlow(config.Name, config.Gui).ConfigureAwait(false);
            }

            return result;
        }
    }
}
