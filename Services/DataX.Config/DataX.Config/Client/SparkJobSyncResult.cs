// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using DataX.Config.Utility;
using System;
using System.Collections.Generic;

namespace DataX.Config
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SparkJobSyncResult
    {
        public const string FieldName_State = "state";
        public const string FieldName_ClientCache = "batch";
        public const string FieldName_Links = "links";
        public const string FieldName_Notes = "note";

        /// <summary>
        /// id of the job
        /// </summary>
        [JsonProperty("id")]
        public string JobId { get; set; }

        /// <summary>
        /// state of the job
        /// </summary>
        [JsonProperty(FieldName_State)]
        [JsonConverter(typeof(StringEnumConverter))]
        public JobState JobState { get; set; }

        /// <summary>
        /// links to the job page on spark
        /// </summary>
        [JsonProperty(FieldName_Links, NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> Links { get; set; }

        /// <summary>
        /// details about the job status
        /// </summary>
        [JsonProperty(FieldName_Notes, NullValueHandling = NullValueHandling.Ignore)]
        public string Note { get; set; }

        /// <summary>
        /// cache state for the spark job client
        /// </summary>
        [JsonProperty(FieldName_ClientCache)]
        public JToken ClientCache { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if(!(obj is SparkJobSyncResult))
            {
                return false;
            }

            var target = (SparkJobSyncResult)obj;

            if(JobState != target.JobState)
            {
                return false;
            }

            if(!Comparison.DictionaryEquals(Links, target.Links))
            {
                return false;
            }

            if (Note != target.Note)
            {
                return false;
            }

            if (ClientCache?.ToString() != target.ClientCache?.ToString())
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(JobId, JobState, Links, Note, ClientCache);
        }
    }
}
