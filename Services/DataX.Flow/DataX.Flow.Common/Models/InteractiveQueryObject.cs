// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using System.Collections.Generic;

namespace DataX.Flow.Common.Models
{
    public class InteractiveQueryObject
    {
        [JsonProperty("subscription")]
        public string Subscription;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("displayName")]
        public string DisplayName;

        [JsonProperty("userName")]
        public string UserName;

        [JsonProperty("inputSchema")]
        public string InputSchema;

        [JsonProperty("query")]
        public string Query;

        [JsonProperty("kernelId")]
        public string KernelId;

        [JsonProperty("normalizationSnippet")]
        public string NormalizationSnippet;
        
        [JsonProperty("eventhubConnectionString")]
        public string EventhubConnectionString;

        [JsonProperty("inputSubscriptionId")]
        public string InputSubscriptionId;

        [JsonProperty("inputResourceGroup")]
        public string InputResourceGroup;

        [JsonProperty("eventhubNames")]
        public string EventhubNames;

        [JsonProperty("inputType")]
        public string InputType; // "Event", "Iothub", "KafkaEventhub", "Kafka"

        [JsonProperty("seconds")]
        public int Seconds;

        [JsonProperty("refData")]
        public List<ReferenceDataObject> ReferenceDatas;

        [JsonProperty("functions")]
        public List<FunctionObject> Functions;
    }
}
