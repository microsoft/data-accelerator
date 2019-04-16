// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataX.Config.ConfigDataModel
{
    public class MetricSource
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("input")]
        public MetricSourceInput Input { get; set; }

        [JsonProperty("output")]
        public MetricSourceOutput Output { get; set; }
    }
}
