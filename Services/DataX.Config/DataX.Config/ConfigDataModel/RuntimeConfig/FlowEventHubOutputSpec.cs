// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.Config.ConfigDataModel.RuntimeConfig
{
   public class FlowEventHubOutputSpec
    {
        [JsonProperty("connectionStringRef")]
        public string ConnectionStringRef { get; set; }

        [JsonProperty("compressionType")]
        public string CompressionType { get; set; }

        [JsonProperty("format")]
        public string Format { get; set; }
    }
}
