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
    public class FlowHttpOutputSpec
    {
        [JsonProperty("endpoint")]
        public string Endpoint { get; set; }

        [JsonProperty("filter")]
        public string Filter { get; set; }

        [JsonProperty("appendHeaders")]
        public IDictionary<string,string> Headers { get; set; }
    }

}


