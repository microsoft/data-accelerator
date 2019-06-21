// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;

namespace DataX.Config.ConfigDataModel
{
    public class FlowGuiInputBatchingInput
    {
        [JsonProperty("inputConnection")]
        public string InputConnection { get; set; }

        [JsonProperty("inputPath")]
        public string InputPath { get; set; }

        [JsonProperty("inputFormatType")]
        public string InputFormatType { get; set; }

        [JsonProperty("inputCompressionType")]
        public string InputCompressionType { get; set; }
    }
}
