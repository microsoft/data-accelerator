// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using System;

namespace DataX.Config.ConfigDataModel
{
    public class FlowGuiInputBatchJobProperties
    {
        [JsonProperty("interval")]
        public string Interval { get; set; }

        [JsonProperty("intervalType")]
        public string IntervalType { get; set; }

        [JsonProperty("delay")]
        public string Delay { get; set; }

        [JsonProperty("delayType")]
        public string DelayType { get; set; }

        [JsonProperty("window")]
        public string Window { get; set; }

        [JsonProperty("windowType")]
        public string WindowType { get; set; }

        [JsonProperty("startTime")]
        public DateTime? StartTime { get; set; }

        [JsonProperty("endTime")]
        public DateTime? EndTime { get; set; }

        [JsonProperty("lastProcessedTime")]
        public string LastProcessedTime { get; set; }
    }
}
