// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace DataX.Config.ConfigDataModel
{
    public class FlowCommonProcessor
    {
        [JsonProperty("sparkJobConfigFolder")]
        public string SparkJobConfigFolder { get; set; }

        [JsonProperty("template")]
        public JObject Template { get; set; }

        [JsonProperty("sparkJobTemplateRef")]
        public string SparkJobTemplateRef { get; set; }

        [JsonProperty("jobCommonTokens")]
        public IDictionary<string, string> JobCommonTokens { get; set; }

        [JsonProperty("jobs")]
        public IList<IDictionary<string, string>> Jobs { get; set; }
    }
}
