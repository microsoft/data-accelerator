// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using System.Collections.Generic;

namespace DataX.Config.ConfigDataModel.RuntimeConfig
{
    public class JarUdfSpec
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("class")]
        public string Class { get; set; }

        [JsonProperty("libs")]
        public IList<string> LibPaths { get; set; }
    }
}
