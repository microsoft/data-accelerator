using DataX.Contract;
using DataX.Utility.ServiceCommunication;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DataX.Flow.Scheduler
{
    public class TimedScheduler : BackgroundService
    {
        private readonly ILogger _logger;
        // Frequency at which to run the scheduler
        private readonly int _schedulerWakeupFrequencyInMin = 60;
        private readonly int _oneMinInMilliSeconds = 60 * 1000;

        internal static InterServiceCommunicator Communicator
        {
            private get;
            set;
        } = new InterServiceCommunicator(new TimeSpan(0, 4, 0));

        public TimedScheduler(ILogger<TimedScheduler> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"{DateTime.UtcNow}:TimedScheduler is starting.");

            stoppingToken.Register(() =>
               _logger.LogInformation($"{DateTime.UtcNow}: TimedScheduler background task is stopping."));

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"{DateTime.UtcNow}:TimedScheduler task doing background work.");

                await StartBatchJobs();

                await Task.Delay(20 * 1000, stoppingToken);
                //await Task.Delay(_schedulerWakeupFrequencyInMin * _oneMinInMilliSeconds, stoppingToken);
            }

            _logger.LogInformation($"{DateTime.UtcNow}: TimedScheduler background task is stopping.");
        }

        private async Task StartBatchJobs()
        {
            _logger.LogInformation($"{ DateTime.UtcNow}:TimedScheduler Starting batch jobs");

            try
            {
                await Communicator.InvokeServiceAsync(HttpMethod.Post, "DataX.Flow", "Flow.ManagementService", "flow/schedulebatch");
            }
            catch(Exception e)
            {
                var message = e.InnerException == null ? e.Message : e.InnerException.Message;
                _logger.LogInformation($"{ DateTime.UtcNow}:TimedScheduler an exception is thrown:" + message);
            }
        }
    }
}
