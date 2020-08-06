namespace Flow.Management.Controllers
{
    public class WaitTillJobsRunningRequest
    {
        public int MaxRetryTimes { get; set; } = 10;

        public int IntervalsBetweenRetriesInSeconds { get; set; } = 5;

        public string[] JobNames { get; set; } = null;
    }
}
