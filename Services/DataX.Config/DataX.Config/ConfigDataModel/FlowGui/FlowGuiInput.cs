// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;

namespace DataX.Config.ConfigDataModel
{
    public class FlowGuiInput
    {
        [JsonProperty("mode")]
        public string Mode { get; set; }

        [JsonProperty("type")]
        public string InputType { get; set; }

        [JsonProperty("properties")]
        public FlowGuiInputProperties Properties { get; set; }

        [JsonProperty("referenceData")]
        public FlowGuiReferenceData[] ReferenceData { get; set; }

        [JsonProperty("batch")]
        public FlowGuiInputBatchInput[] Batch { get; set; }
    }
}
