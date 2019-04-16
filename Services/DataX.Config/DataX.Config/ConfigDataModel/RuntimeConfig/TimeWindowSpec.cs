// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;

namespace DataX.Config.ConfigDataModel.RuntimeConfig
{
    public class TimeWindowSpec
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("windowDuration")]
        public string WindowDuration { get; set; }
    }
}
