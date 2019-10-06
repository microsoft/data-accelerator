// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

using Microsoft.Azure.Cosmos.Table;
using System;
using DataX.Utilities;

namespace JobRunner
{
    /// <summary>
    /// Represents a Scheduled Job definition identified by its <see cref="Name"/>.
    /// </summary>
    public sealed class ScheduledJobEntity : BaseTableEntity
    {
        /// <summary>
        /// The unique name to give a scheduled job.
        /// </summary>
        public string Name
        {
            get => RowKey;
            set => RowKey = value;
        }

        /// <summary>
        /// The type of job to run.
        /// </summary>
        public string JobKey
        {
            get;
            set;
        }

        /// <summary>
        /// The date/time to begin running this scheduled job.
        /// </summary>
        public DateTimeOffset StartRunningAt
        {
            get;
            set;
        }

        /// <summary>
        /// The last date/time this job was run.
        /// </summary>
        public DateTimeOffset LastRunAt
        {
            get;
            set;
        }

        /// <summary>
        /// The interval between scheduled job runs.
        /// </summary>
        [IgnoreProperty]
        public TimeSpan Interval
        {
            get => new TimeSpan(0, 0, IntervalSeconds);
            set => IntervalSeconds = (int)value.TotalSeconds;
        }

        /// <summary>
        /// This is the interval in seconds between scheduled job runs.
        /// Table storage can't actually handle Timespans, so this is the underlying value for Interval.
        /// </summary>
        public int IntervalSeconds
        {
            get;
            set;
        }

        /// <summary>
        /// Should the job be scheduled on the primary queue or the test queue.  Defaults to false.
        /// </summary>
        public bool UseTestQueue
        {
            get;
            set;
        }


        [IgnoreProperty]
        public bool ShouldRun => TimeSinceLastRun > Interval;

        [IgnoreProperty]
        private DateTimeOffset LatestIntervalStart => LastRunAt == new DateTimeOffset() ? StartRunningAt : LastRunAt;

        [IgnoreProperty]
        private TimeSpan TimeSinceLastRun => DateTimeOffset.UtcNow - LatestIntervalStart;

        /// <summary>
        /// For use when queuing the schedule, to prevent multiple workers double-queuing
        /// </summary>
        [IgnoreProperty]
        public string DeduplicationId => (LatestIntervalStart.ToUnixTimeSeconds() + IntervalSeconds).ToString();

        public void IncrementLastRunAt()
        {
            // For the purpose of always trying to run at clean intervals of time and not churn on catchup,
            // this makes it such that if e.g. 5.5 intervals have passed, we just run once and set the time at 5 intervals passed.
            LastRunAt = LatestIntervalStart.AddSeconds(Math.Floor(TimeSinceLastRun.TotalSeconds / Interval.TotalSeconds) * Interval.TotalSeconds);
        }

        public ScheduledJobEntity()
        {
            PartitionKey = SharedPartitionKey;
        }

        internal static readonly string SharedPartitionKey = "sharedPartitionKey";

        public ScheduledJobEntity(string name, string jobKey, DateTimeOffset startAt, TimeSpan interval, bool useTestQueue = false)
        {
            Name = name;
            JobKey = jobKey;
            StartRunningAt = startAt;
            Interval = interval;
            PartitionKey = SharedPartitionKey;
            UseTestQueue = useTestQueue;
        }
    }
}
