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
   public class FlowCosmosDbOutputSpec
    {
        [JsonProperty("connectionStringRef")]
        public string ConnectionStringRef { get; set; }

        [JsonProperty("database")]
        public string Database { get; set; }

        [JsonProperty("collection")]
        public string Collection { get; set; }
    }
}
