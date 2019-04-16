// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
namespace DataX.Metrics.Ingestor.Helper
{
    /// <summary>
    /// An object for metric input
    /// </summary>
    public class MetricItem
    {
        // Data EventTime
        public string EventTime;

        // TableName
        public string MetricName;

        // MetricValue
        public string Metric;

        // FlowId
        public string Product;

        // Optional. Only the custom metrics need this 
        public string Pivot;
    }
}
