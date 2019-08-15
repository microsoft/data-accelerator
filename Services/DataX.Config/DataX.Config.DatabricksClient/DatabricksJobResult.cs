// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.Config.DatabricksClient
{
    public class DatabricksJobResult
    {
        [JsonProperty("job_id")]
        public int JobId { get; set; }

        [JsonProperty("run_id")]
        public int RunId { get; set; }

        [JsonProperty("state")]
        public Dictionary<string, string> State { get; set; }
    }
}
