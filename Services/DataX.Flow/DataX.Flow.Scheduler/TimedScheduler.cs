using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
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

                 StartBatchJobs();
               
                await Task.Delay(_schedulerWakeupFrequencyInMin * _oneMinInMilliSeconds, stoppingToken);
            }

            _logger.LogInformation($"{DateTime.UtcNow}: TimedScheduler background task is stopping.");
        }

        private void StartBatchJobs()
        {
            _logger.LogInformation("Starting batch jobs");

            /*
               Call ScheduleBatch() API
             */
        }
    }
}
