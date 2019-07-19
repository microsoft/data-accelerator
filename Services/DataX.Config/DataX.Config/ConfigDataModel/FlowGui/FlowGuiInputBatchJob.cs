// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;

namespace DataX.Config.ConfigDataModel
{
    public class FlowGuiInputBatchJob
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("properties")]
        public FlowGuiInputBatchJobProperties Properties { get; set; }

        [JsonProperty("disabled")]
        public bool Disabled { get; set; }
    }
}
