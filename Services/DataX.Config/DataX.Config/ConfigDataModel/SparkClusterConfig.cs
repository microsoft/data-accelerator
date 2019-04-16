// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using System;

namespace DataX.Config.ConfigDataModel
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SparkClusterConfig: EntityConfig
    {
        public static SparkClusterConfig From(JsonConfig json)
        {
            return ConvertFrom<SparkClusterConfig>(json);
        }

        public static SparkClusterConfig From(string json)
        {
            return From(JsonConfig.From(json));
        }

        [JsonProperty("connectionString")]
        public string ConnectionString { get; set; }
    }
}
