// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;

namespace DataX.Config.ConfigDataModel
{
    public class FlowGuiInputProperties
    {
        [JsonProperty("inputEventhubName")]
        public string InputEventHubName { get; set; }

        [JsonProperty("inputEventhubConnection")]
        public string InputEventhubConnection { get; set; }

        [JsonProperty("windowDuration")]
        public string WindowDuration { get; set; }

        [JsonProperty("timestampColumn")]
        public string TimestampColumn { get; set; }

        [JsonProperty("watermarkValue")]
        public string WatermarkValue { get; set; }

        [JsonProperty("watermarkUnit")]
        public string WatermarkUnit { get; set; }

        [JsonProperty("maxRate")]
        public string MaxRate { get; set; }

        [JsonProperty("inputSchemaFile")]
        public string InputSchemaFile { get; set; }

        [JsonProperty("showNormalizationSnippet")]
        public bool ShowNormalizationSnippet { get; set; }

        [JsonProperty("normalizationSnippet")]
        public string NormalizationSnippet { get; set; }

        [JsonProperty("inputSubscriptionId")]
        public string InputSubscriptionId { get; set; }

        [JsonProperty("inputResourceGroup")]
        public string InputResourceGroup { get; set; }
    }
}
