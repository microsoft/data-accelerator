// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;

namespace DataX.Config.ConfigDataModel
{
    public class FlowGuiReferenceData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("typeDisplay")]
        public string TypeDisplay { get; set; }

        [JsonProperty("properties")]
        public FlowGuiReferenceDataProperties Properties { get; set; }
    }
}
