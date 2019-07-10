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
    public class FlowOutputSpec
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("blob", NullValueHandling =NullValueHandling.Ignore)]
        public FlowBlobOutputSpec BlobOutput{ get; set; }

        [JsonProperty("cosmosdb", NullValueHandling = NullValueHandling.Ignore)]
        public FlowCosmosDbOutputSpec CosmosDbOutput { get; set; }

        [JsonProperty("eventhub", NullValueHandling = NullValueHandling.Ignore)]
        public FlowEventHubOutputSpec EventHubOutput { get; set; }

        [JsonProperty("httppost", NullValueHandling = NullValueHandling.Ignore)]
        public FlowHttpOutputSpec HttpOutput { get; set; }

        [JsonProperty("sqlServer", NullValueHandling = NullValueHandling.Ignore)]
        public FlowSqlOutputSpec SqlOutput { get; set; }

    }
}
