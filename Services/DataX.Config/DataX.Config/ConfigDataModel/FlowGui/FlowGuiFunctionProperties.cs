// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using System.Collections.Generic;

namespace DataX.Config.ConfigDataModel
{
    public class FlowGuiFunctionProperties
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        // Jar UDF and UDAF properties
        [JsonProperty("path", NullValueHandling = NullValueHandling.Ignore)]
        public string Path { get; set; }

        [JsonProperty("class", NullValueHandling = NullValueHandling.Ignore)]
        public string Class { get; set; }

        [JsonProperty("libs", NullValueHandling = NullValueHandling.Ignore)]
        public IList<string> LibPaths { get; set; }


        // Azure Function properties
        [JsonProperty("serviceEndpoint", NullValueHandling = NullValueHandling.Ignore)]
        public string ServiceEndpoint { get; set; }

        [JsonProperty("api", NullValueHandling = NullValueHandling.Ignore)]
        public string Api { get; set; }

        [JsonProperty("code", NullValueHandling = NullValueHandling.Ignore)]
        public string Code { get; set; }

        [JsonProperty("methodType", NullValueHandling = NullValueHandling.Ignore)]
        public string MethodType { get; set; }

        [JsonProperty("params", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> ParameterNames { get; set; }
    }
}
