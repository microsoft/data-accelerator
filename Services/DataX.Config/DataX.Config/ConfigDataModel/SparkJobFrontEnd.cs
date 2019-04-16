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
    public class SparkJobFrontEnd
    {
        public static SparkJobFrontEnd FromSparkJobConfig(SparkJobConfig job)
        {
            if (job == null)
            {
                return null;
            }

            return new SparkJobFrontEnd()
            {
                Name = job.Name,
                JobState = job.SyncResult?.JobState??JobState.Idle,
                Links = job.SyncResult?.Links,
                Cluster = job.Cluster,
                Note = job.SyncResult?.Note
            };
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("cluster")]
        public string Cluster { get; set; }

        [JsonProperty("state")]
        [JsonConverter(typeof(StringEnumConverter))]
        public JobState JobState { get; set; }

        [JsonProperty("links")]
        public Dictionary<string, string> Links { get; set; }

        [JsonProperty("note")]
        public string Note { get; set; }
    }
}
