// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;

namespace DataX.Config.ConfigDataModel
{
    public class FlowGuiInputBatchInputProperties
    {
        [JsonProperty("connection")]
        public string Connection { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("formatType")]
        public string FormatType { get; set; }

        [JsonProperty("compressionType")]
        public string CompressionType { get; set; }
    }
}
