// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using System.Collections.Generic;

namespace DataX.Config.ConfigDataModel
{
    public class MetricSourceOutput
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("data")]
        private Dictionary<string, bool> DataToggles { get; set; }

        [JsonProperty("chartTimeWindowInMs", NullValueHandling = NullValueHandling.Ignore)]
        public long? ChartTimeWindowInMs { get; set; }

        [JsonProperty("dynamicOffsetInMs", NullValueHandling = NullValueHandling.Ignore)]
        public long? DynamicOffsetInMs { get; set; }
    }
}
