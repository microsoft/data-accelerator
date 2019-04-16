// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
namespace DataX.Metrics.Ingestor.Helper
{
    /// <summary>
    /// An object for metric outputs 
    /// </summary>
    public class MetricOutput
    {
        // RedisKey. Format: <FlowId>:<MetricName>
        public string RedisKey;

        // Content. Format: uts:<EventTime>, val:<Metric>, pivot1: ""
        public string Content;

        // Redis score. epoch time (utc time - epochzero)
        public double EpochTime;
    }
}
