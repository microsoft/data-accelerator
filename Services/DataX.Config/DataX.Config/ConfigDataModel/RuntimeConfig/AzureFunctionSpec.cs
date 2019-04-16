// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using System.Collections.Generic;

namespace DataX.Config.ConfigDataModel.RuntimeConfig
{
    public class AzureFunctionSpec
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("serviceEndpoint")]
        public string ServiceEndpoint { get; set; }

        [JsonProperty("api")]
        public string Api { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("methodType")]
        public string MethodType { get; set; }

        [JsonProperty("params")]
        public IList<string> ParameterNames { get; set; }
    }
}
