// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.Config.LivyClient
{
    public class LivyBatchResult
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("appId")]
        public string AppId { get; set; }

        [JsonProperty("appInfo")]
        public Dictionary<string, string> AppInfo { get; set; }

        [JsonProperty("log")]
        public string[] Log { get; set; }
    }
}
