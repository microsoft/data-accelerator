// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.Flow.Common.Models
{
    public class FunctionObject
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("properties")]
        public object Properties { get; set; }

        [JsonProperty("typeDisplay")]
        public string TypeDisplay { get; set; }
    }

    public class PropertiesUD
    {
        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("class")]
        public string ClassName { get; set; }

        [JsonProperty("libs")]
        public List<string> Libs { get; set; }
    }
}
