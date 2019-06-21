// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;

namespace DataX.Config.ConfigDataModel
{
    public class FlowGuiInputBatchingJob
    {
        [JsonProperty("interval")]
        public string Interval { get; set; }

        [JsonProperty("offset")]
        public string Offset { get; set; }

        [JsonProperty("window")]
        public string Window { get; set; }

        [JsonProperty("startTime")]
        public string StartTime { get; set; }

        [JsonProperty("endTime")]
        public string EndTime { get; set; }

        [JsonProperty("lastProcessedTime")]
        public string LastProcessedTime { get; set; }

        [JsonProperty("disabled")]
        public bool Disabled { get; set; }
    }
}
