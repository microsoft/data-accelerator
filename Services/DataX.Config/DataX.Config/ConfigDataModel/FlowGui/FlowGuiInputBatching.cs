// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;

namespace DataX.Config.ConfigDataModel
{
    public class FlowGuiInputBatching
    {
        [JsonProperty("recurring")]
        public FlowGuiInputBatchingJob Recurring { get; set; }

        [JsonProperty("onetime")]
        public FlowGuiInputBatchingJob[] Onetime { get; set; }

        [JsonProperty("inputs")]
        public FlowGuiInputBatchingInput[] Inputs { get; set; }
    }
}
