// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;

namespace DataX.Config.ConfigDataModel
{
    public class MetricWidget
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("data")]
        public string DataContract { get; set; }

        [JsonProperty("formatter", NullValueHandling = NullValueHandling.Ignore)]
        public string DataFormatter { get; set; }

        [JsonProperty("position")]
        public string Position { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
