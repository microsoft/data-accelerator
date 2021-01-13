namespace Flow.Management.Controllers
{
    public class RestatAllWithRetriesRequest
    {
        public string[] JobNames { get; set; } = null;

        public int MaxRetryTimes { get; set; } = 2; 

        public int WaitIntervalInSeconds { get; set; } = 2;

        public int MaxWaitTimeInSeconds { get; set; } = 60;
    }
}
