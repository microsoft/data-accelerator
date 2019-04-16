// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.Config.ConfigDataModel
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MetricsConfig : EntityConfig
    {
        [JsonProperty("sources")]
        public MetricSource[] Sources { get; set; }

        [JsonProperty("widgets")]
        public MetricWidget[] Widgets { get; set; }

        [JsonProperty("initParameters")]
        public Dictionary<string, JToken> InitialParameters { get; set; }
    }
}
