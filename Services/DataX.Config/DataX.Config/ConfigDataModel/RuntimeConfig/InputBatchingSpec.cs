// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using System.Collections.Generic;

namespace DataX.Config.ConfigDataModel.RuntimeConfig
{
    public class InputBatchingSpec
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("format")]
        public string Format { get; set; }

        [JsonProperty("compressiontype")]
        public string CompressionType { get; set; }

        [JsonProperty("processstarttime")]
        public string ProcessStartTime { get; set; }

        [JsonProperty("processendtime")]
        public string ProcessEndTime { get; set; }

        [JsonProperty("partitionincrement")]
        public string PartitionIncrement { get; set; }
    }
}
