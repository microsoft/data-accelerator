// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DataX.Config.Local
{
    /// <summary>
    /// Spark client to manage local spark jobs
    /// </summary>
    public class LocalSparkClient : ISparkJobClient
    {
        private readonly string _sparkSubmitCmd;
        private readonly string _sparkSubmitArgs = @" --class  {0} --master local[*]  --jars {1} {2} {3}";
        private readonly string _sparkHomeFolder;
        public const string ConfigSettingName_SparkHomeFolder = "SparkHome";
        public const string ConfigSettingName_LocalRootFolder = "localRoot";
        private ILogger _logger { get; }

        public LocalSparkClient(ILogger logger)
        {
            _sparkHomeFolder = InitialConfiguration.Get(ConfigSettingName_SparkHomeFolder);
            _sparkSubmitCmd = Path.Combine(_sparkHomeFolder, "bin", "spark-submit");
            _logger = logger;
        }

        public Task<SparkJobSyncResult> SubmitJob(JToken jobSubmissionData)
        {
            var jobSubmitInfo = ParseJobSubmitData(jobSubmissionData);
            var batchResult = StartLocalSparkJob(jobSubmitInfo);

            if (batchResult.Id > 0)
            {
                _logger.LogInformation($"Started local job with process id {batchResult.Id.ToString()}");

                return Task.FromResult(new SparkJobSyncResult()
                {
                    JobId = batchResult.Id.ToString(),
                    JobState = JobState.Running,
                    ClientCache = JToken.FromObject(batchResult)
                });
            }
            else
            {
                return Task.FromResult(GetDefaultSparkJobInfo());
            }
        }

        public Task<SparkJobSyncResult> StopJob(JToken jobClientData)
        {
            var batch = jobClientData.ToObject<LocalBatchResult>();

            StopLocalSparkJob(batch);

            return Task.FromResult(GetDefaultSparkJobInfo());
        }

        public Task<SparkJobSyncResult> GetJobInfo(JToken jobClientData)
        {
            var batch = jobClientData.ToObject<LocalBatchResult>();

            if (batch == null || batch.Id < 0)
            {
                return Task.FromResult(GetDefaultSparkJobInfo());
            }
            else
            {
                try
                {
                    var jobProc = Process.GetProcessById(batch.Id);
                    var currentJobState = IsSparkJobProcess(jobProc, batch) ? JobState.Running : JobState.Idle;

                    _logger.LogInformation($"CurrentJobState is {currentJobState}");

                    return Task.FromResult(new SparkJobSyncResult()
                    {
                        JobId = batch.Id.ToString(),
                        JobState = currentJobState,
                        ClientCache = JToken.FromObject(batch)
                    });
                }
                catch (Exception ex)
                {
                    // If job process is stopped outside of the DataX UI/API, then GetProcessId call may fail. Handle this case
                    // and set the jobs state to default
                    _logger.LogWarning($"Job process is not running anymore. GetProcessId failed with msg: {ex.Message}");
                }

                // Since job is not running anymore, set its state to default.
                return Task.FromResult(GetDefaultSparkJobInfo());
            }
        }

        public Task<SparkJobSyncResult[]> GetJobs()
        {
            throw new NotImplementedException();
        }


        private LocalBatchResult StartLocalSparkJob(LocalJobSubmissionInfo jobSubmitData)
        {
            string args = string.Format(_sparkSubmitArgs, jobSubmitData.ClassName, jobSubmitData.Jars, jobSubmitData.FileName, jobSubmitData.Config);

            _logger.LogInformation($"Starting local job command {_sparkSubmitCmd} with args {args}");
            var jobProcess = ExecuteSparkSubmitCommand(_sparkSubmitCmd, args);

            return new LocalBatchResult()
            {
                Id = jobProcess.HasExited ? -1 : jobProcess.Id,
                Name = jobProcess.HasExited ? null : jobProcess.ProcessName,
                StartTime = jobProcess.HasExited ? DateTime.MinValue : jobProcess.StartTime
            };
        }

        private bool StopLocalSparkJob(LocalBatchResult batch)
        {
            try
            {
                _logger.LogInformation($"Stopping local job with process id {batch.Id.ToString()}");
                var jobProcess = Process.GetProcessById(batch.Id);

                if (IsSparkJobProcess(jobProcess, batch))
                {
                    var result = jobProcess.CloseMainWindow();
                    // Kill the process if CloseMainWindow is not supported.
                    // This is true for linux where shellExecute is set to false
                    if (!result && !jobProcess.HasExited)
                    {
                        _logger.LogInformation($"Killing the process with id {jobProcess.Id}");
                        jobProcess.Kill();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                // Its possible for job process to be stopped outside of the flow. Handle this case.
                _logger.LogWarning($"Job process is likely not running anymore. GetProcessById failed with msg: {ex.Message}");
            }

            return false;
        }

        private Process ExecuteSparkSubmitCommand(string command, string args)
        {
            // UseShellExecute for Windows only. In non-Windows there is a active .NETCore bug
            // that prevents Process.Start to start the command script due to path resolution issue.
            // For more details see https://github.com/dotnet/core/issues/1857

            bool useShellExecute = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? true : false;

            var processInfo = new ProcessStartInfo(command)
            {
                UseShellExecute = useShellExecute,
                Arguments = args
            };
            var process = Process.Start(processInfo);
            return process;
        }

        /// <summary>
        /// Check if the given process is same as the spark job process.
        /// </summary>
        /// <param name="jobProc"></param>
        /// <param name="batch"></param>
        /// <returns></returns>
        private bool IsSparkJobProcess(Process jobProc, LocalBatchResult batch)
        {
            // Compare process id and startTime to determine if the spark job that was started is same as the one passed in
            return jobProc != null && !jobProc.HasExited && batch != null && jobProc.Id == batch.Id && jobProc.StartTime == batch.StartTime;
        }


        private LocalJobSubmissionInfo ParseJobSubmitData(JToken jobSubmissionData)
        {
            var args = jobSubmissionData?.SelectToken("args") as JArray;
            var jars = jobSubmissionData?.SelectToken("jars") as JArray;
            var jarsStr = string.Join(",", jars);

            return new LocalJobSubmissionInfo()
            {
                ClassName = jobSubmissionData?.SelectToken("className")?.ToString(),
                Jars = jarsStr,
                FileName = jobSubmissionData?.SelectToken("file")?.ToString(),
                Config = args[0]?.ToString()
            };
        }

        private SparkJobSyncResult GetDefaultSparkJobInfo()
        {
            return new SparkJobSyncResult()
            {
                JobId = null,
                JobState = JobState.Idle,
                ClientCache = JToken.FromObject("")
            };
        }
    }
}
