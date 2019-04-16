// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataX.Config.ConfigDataModel
{
    public class MetricSourceInput
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("pollingInterval", NullValueHandling = NullValueHandling.Ignore)]
        public int? PollingInterval { get; set; }

        [JsonProperty("metricKeys")]
        public JToken[] DataKeys { get; set; }
    }
}
