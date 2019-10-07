// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace DataX.Config.ConfigDataModel
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SparkJobConfig : EntityConfig
    {
        internal const string FieldName_SyncResult = "syncResult";

        public static SparkJobConfig From(JsonConfig json)
        {
            return ConvertFrom<SparkJobConfig>(json);
        }

        public static SparkJobConfig From(string json)
        {
            return From(JsonConfig.From(json));
        }

        /// <summary>
        /// Name of the job
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// cluster to run the job
        /// </summary>
        [JsonProperty("cluster")]
        public string Cluster { get; set; }

        /// <summary>
        /// Path to key vault secret that stores databricks token
        /// </summary>
        [JsonProperty("databricksToken")]
        public string DatabricksToken { get; set; }

        /// <summary>
        /// options for submitting the job
        /// </summary>
        [JsonProperty("options")]
        public JObject Options { get; set; }

        /// <summary>
        /// state object from last sync
        /// </summary>
        [JsonProperty(FieldName_SyncResult)]
        public SparkJobSyncResult SyncResult { get; set; }
    }
}
