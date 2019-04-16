// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.Config.ConfigDataModel.CommonData
{
    public class CommonDataConfig : EntityConfig
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("content")]
        public JToken Content { get; set; }
    }
}
