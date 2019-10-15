// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

using DataX.Utilities.KeyVault;
using DataX.Utilities.Storage;
using JobRunner.Jobs;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JobRunner
{
    [Export]
    public class JobRunner
    {
        // We have a distinct primary and test queue to compartmentalize runners.  The target queue client for this runner is set in the configuration.
        private readonly IQueueClient _primaryQueueClient;
        private readonly IQueueClient _testQueueClient;
        private const int _Minutes = 20;

        private readonly string _activeQueueName;
        private IQueueClient _ActiveQueueClient
        {
            get
            {
                if (_activeQueueName == _primaryQueueClient.QueueName)
                {
                    return _primaryQueueClient;
                }
                return _testQueueClient;
            }
        }

        private readonly ILogger<JobRunner> _logger;

        // We DI our jobs so they can get requirements "for free"
        private readonly IServiceCollection _services = new ServiceCollection();
        private readonly IServiceProvider _provider;

        public ScheduledJobTable ScheduledJobTable { get; }

        // Used for mapping serialized job names off of service bus.
        private readonly IDictionary<string, Type> _jobKeyTypeMappings = new Dictionary<string, Type>();

        [ImportingConstructor]
        public JobRunner(IConfiguration configuration, ScheduledJobTable jobTable)
        {
            IServiceCollection services = new ServiceCollection();
            var appConfig = new AppConfig(configuration);
            services.AddLogging(loggingBuilder =>
            {
                // Optional: Apply filters to configure LogLevel Trace or above is sent to ApplicationInsights for all
                // categories.
                loggingBuilder.AddFilter<ApplicationInsightsLoggerProvider>("", LogLevel.Information);
                var abc = GetInstrumentationKey(appConfig);
                loggingBuilder.AddApplicationInsights(GetInstrumentationKey(appConfig));
            });
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            _logger = serviceProvider.GetRequiredService<ILogger<JobRunner>>();
            
            var sbConnection = KeyVault.GetSecretFromKeyvault(appConfig.ServiceBusConnectionString);

            _primaryQueueClient = new QueueClient(sbConnection, appConfig.PrimaryQueueName);
            _testQueueClient = new QueueClient(sbConnection, appConfig.TestQueueName);
            _activeQueueName = appConfig.ActiveQueueName;

            // NOTE: If you need to DI other things (e.g. for your jobs) here's where you'd do it, and then just add the injected class into your constructor.
            _services.AddSingleton<AppConfig>(appConfig)
             .AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions()
             {
                 EnableAdaptiveSampling = false,
                 EnableDebugLogger = false,
                 InstrumentationKey = GetInstrumentationKey(appConfig)
             })
             .AddLogging(logging =>
             {
                 try
                 {
                     // In order to log ILogger logs
                     logging.AddApplicationInsights();
                     // Optional: Apply filters to configure LogLevel Information or above is sent to
                     // ApplicationInsights for all categories.
                     logging.AddFilter<ApplicationInsightsLoggerProvider>("", LogLevel.Information);

                     // Additional filtering For category starting in "Microsoft",
                     // only Warning or above will be sent to Application Insights.
                     logging.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft", LogLevel.Warning);

                 }
                 catch (Exception e)
                 {                     
                 }
             });
            _services.AddSingleton<ILogger>(_logger);
            
            LoadJobTypes();
            _provider = _services.BuildServiceProvider();
            this.ScheduledJobTable = jobTable;
        }
        private static string GetInstrumentationKey(AppConfig settings)
        {
            var secretName = settings?.AppInsightsIntrumentationKey;
            var vaultName = settings.ServiceKeyVaultName;

            return string.IsNullOrWhiteSpace(secretName) || string.IsNullOrWhiteSpace(vaultName)
                ? Guid.Empty.ToString()
                : KeyVault.GetSecretFromKeyvault(settings.ServiceKeyVaultName, settings.AppInsightsIntrumentationKey);
        }

        /// <summary>
        /// For a Job type (inheriting from IJob) returns a string name (the class name) for serializing into the job queue
        /// </summary>
        /// <typeparam name="T">The Job Type (inheriting from IJob) to fetch the string name of.</typeparam>
        /// <returns>A string representing the name of the specified Job type.</returns>
        private static string GetJobKey<T>() where T : IJob
        {
            return typeof(T).Name;
        }

        /// <summary>
        /// Created a new instance of the job type specified by the serialized key string.
        /// This key string is generated via GetJobKey.
        /// </summary>
        /// <param name="key">String-form name of the Job type for serialization in job queue</param>
        /// <returns>Instance of the Job type requested.</returns>
        private IJob CreateJobByKey(string key)
        {
            if (_jobKeyTypeMappings.TryGetValue(key, out Type job_type))
            {
                var job = (IJob)_provider.GetService(job_type);
                return job;
            }
            return null;
        }

        /// <summary>
        /// Enumerates JobRunner.Jobs namespace for all job types and prepares them to be DI initialized.
        /// To expose a new Job type, simply add a new Job to the Jobs folder, inheriting from IJob. (See TestJob)
        /// </summary>
        private void LoadJobTypes() {
            var assembly = Assembly.GetExecutingAssembly();
            foreach (var type in assembly.GetTypes())
            {
                if (IsJobType(type))
                {
                    _jobKeyTypeMappings.Add(type.Name, type);
                    _services.AddTransient(type);
                }
            }
            _logger.LogInformation($"LoadedJobRunnerJobs: {_jobKeyTypeMappings.Count}");
        }

        /// <summary>
        /// Determine if a given Type is a Job (Inherits from IJob, within JobRunner.Jobs namespace)
        /// </summary>
        /// <param name="type">The type to examine</param>
        /// <returns>A boolean of if this is a job or not.</returns>
        private bool IsJobType(Type type)
        {
            var attr = Attribute.GetCustomAttribute(type, typeof(CompilerGeneratedAttribute));            
            return string.Equals(type.Namespace, "JobRunner.Jobs", StringComparison.Ordinal)
                    && !type.IsInterface && attr==null;
        }

        /// <summary>
        /// Insert a specified Job (from JobRunner.Jobs namespace, inheriting from IJob) into the job queue to be run asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of job to be queued.</typeparam>
        public async Task EnqueueJobAsync<T>(bool useTestQueue=false) where T : IJob
        {
            await EnqueueJobByKeyAsync(GetJobKey<T>(), useTestQueue);
        }

        private async Task EnqueueJobByKeyAsync(string jobKey, bool useTestQueue=false, string messageId=null)
        {
            var payload = new JobQueueMessage() { JobKey = jobKey };
            var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)))
            {
                // We very intentionally don't want to duplicate-detect, so we add ticks as a nonce.
                // For scheduled messages we only send once per interval, we use their timestamp to guarantee sending only once.
                MessageId = string.IsNullOrWhiteSpace(messageId)
                    ? (jobKey + DateTime.UtcNow.Ticks.ToString())
                    : (jobKey + messageId)
            };
            if (useTestQueue)
            {
                await _testQueueClient.SendAsync(message);
            }
            else
            {
                await _primaryQueueClient.SendAsync(message);
            }
        }

        /// <summary>
        /// Callback for a new message appearing on the Queue.  Fetches the job specified by the Message payload,
        /// and attempts to run it.
        /// </summary>
        /// <param name="message">The Message object to process</param>
        /// <param name="token">Cancellation token to halt running</param>
        /// <returns>async task</returns>
        private async Task HandleQueuedJobAsync(Message message, CancellationToken token)
        {
            var body = Encoding.UTF8.GetString(message.Body);
            var payload = JsonConvert.DeserializeObject<JobQueueMessage>(body);
            var job = CreateJobByKey(payload.JobKey);

            _logger.LogInformation("ReceivedJobRunnerJob: " + payload.JobKey.ToString());
            try
            {
                var jobTask = job.RunAsync();
                // Terminate after 5 minute timeout due to servicebus.
                if (await Task.WhenAny(jobTask, Task.Delay(1000 * 60 * 5)) != jobTask)
                {
                    throw new JobRunnerException("JobRunner Job Timeout");
                }
                // TODO: Can in the future allow for specifiable job timeout on job baseclass. (and if >5m, immediately completes on queue)
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"JobRunnerJobFailure: JobType: {payload.JobKey}; Error: {e.Message}");
            }

            await StorageUtils.RetryOnConflictAsync(
                async () =>
                {
                    await _ActiveQueueClient.CompleteAsync(message.SystemProperties.LockToken);
                }, async () =>
                {
                    _logger.LogWarning($"JobRunnerAttemptedToCompleteJob: JobType: {payload.JobKey}");
                    await Task.Yield();
                    return true;
                });
            _logger.LogInformation($"JobRunnerCompletedJob: JobType: {payload.JobKey}");
        }

        private async Task EnqueueScheduledJobsAsync()
        {
            var scheduledJobs = await this.ScheduledJobTable.RetrieveAllAsync();
            _logger.LogInformation($"JobRunnerFetchedScheduledJobs: {scheduledJobs.Count()}");
            foreach (var schedule in scheduledJobs)
            {
                if (schedule.ShouldRun)
                {
                    _logger.LogInformation($"JobRunnerEnqueuingScheduledJob: JobKey: {schedule.JobKey} DeduplicationKey: {schedule.DeduplicationId}");
                    await EnqueueJobByKeyAsync(schedule.JobKey, schedule.UseTestQueue, schedule.DeduplicationId);
                    schedule.IncrementLastRunAt();
                    await this.ScheduledJobTable.ReplaceAsync(schedule);
                }
            }
        }

        /// <summary>
        /// Register scheduled jobs here, in absence of a web management experience.
        /// </summary>
        private void RegisterScheduledJobs()
        {
            this.ScheduledJobTable.CreateOrUpdateScheduledJobAsync(
                "TESTSCHEDULEDJOB",
                GetJobKey<TestJob>(),
                new DateTime(2019, 1, 1, 1, 1, 1),
                new TimeSpan(0, 5, 0),
                useTestQueue: true).Wait();

            // run DataX Schema and Query job every few minutes
            this.ScheduledJobTable.CreateOrUpdateScheduledJobAsync(
                "DataXSchemaAndQueryJob",
                GetJobKey<DataXSchemaAndQueryJob>(),
                new DateTime(2019, 1, 1, 1, 1, 1),
                new TimeSpan(0, _Minutes, 0),
                useTestQueue: true).Wait();

            //run DataX mainline job every few minutes.
            this.ScheduledJobTable.CreateOrUpdateScheduledJobAsync(
                "DataXDeployJob",
                GetJobKey<DataXDeployJob>(),
                new DateTime(2019, 1, 1, 1, 1, 1),
                new TimeSpan(0, _Minutes, 0),
                useTestQueue: true).Wait();
        }

        /// <summary>
        /// Primary entry point for JobRunner.  Typically only called from the webjob, as it will run forever.
        /// </summary>
        public async Task RunForeverAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            RegisterScheduledJobs();

            var options = new MessageHandlerOptions((e) =>
            {
                _logger.LogError(e.Exception, $"JobRunnerMessageHandlerFailure: Error: {e.Exception.Message}");
                return Task.CompletedTask;
            })
            {
                MaxConcurrentCalls = 200,
                AutoComplete = false
            };
            _ActiveQueueClient.RegisterMessageHandler(HandleQueuedJobAsync, options);

            var lastHeartbeat = DateTime.UtcNow;
            var heartbeatInterval = new TimeSpan(0, 1, 0);
            while (!cancellationToken.IsCancellationRequested)
            {
                if (DateTime.UtcNow - lastHeartbeat > heartbeatInterval)
                {                                        
                    lastHeartbeat = DateTime.UtcNow;
                    _logger.LogInformation($"Last Heartbeat: {lastHeartbeat}");
                    await EnqueueScheduledJobsAsync();
                }
            }
        }
    }
}
