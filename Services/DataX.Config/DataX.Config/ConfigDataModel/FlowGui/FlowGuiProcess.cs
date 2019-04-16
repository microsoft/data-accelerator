// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using System.Collections.Generic;

namespace DataX.Config.ConfigDataModel
{
    public class FlowGuiProcess
    {
        [JsonProperty("timestampColumn")]
        public string TimestampColumn { get; set; }

        [JsonProperty("watermark")]
        public string Watermark { get; set; }

        [JsonProperty("functions")]
        public IList<FlowGuiFunction> Functions { get; set; }

        [JsonProperty("queries")]
        public IList<string> Queries { get; set; }

        [JsonProperty("jobconfig")]
        public FlowGuiJobConfig JobConfig { get; set; }
    }
}
