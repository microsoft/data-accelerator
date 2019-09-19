// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;

namespace DataX.Config.ConfigDataModel
{
    // Class representing union schema of all supported ouputs (Blob, CosmosDB, EventHub, SQL Server, Metrics)
    public class FlowGuiOutputProperties
    {
        [JsonProperty("connectionString", NullValueHandling = NullValueHandling.Ignore)]
        public string ConnectionString;

        [JsonProperty("containerName", NullValueHandling = NullValueHandling.Ignore)]
        public string ContainerName;

        [JsonProperty("blobPrefix", NullValueHandling = NullValueHandling.Ignore)]
        public string BlobPrefix;

        [JsonProperty("blobPartitionFormat", NullValueHandling = NullValueHandling.Ignore)]
        public string BlobPartitionFormat;

        [JsonProperty("format", NullValueHandling = NullValueHandling.Ignore)]
        public string Format;

        [JsonProperty("compressionType", NullValueHandling = NullValueHandling.Ignore)]
        public string CompressionType;

        [JsonProperty("db", NullValueHandling = NullValueHandling.Ignore)]
        public string Db;

        [JsonProperty("collection", NullValueHandling = NullValueHandling.Ignore)]
        public string Collection;

        [JsonProperty("tableName", NullValueHandling = NullValueHandling.Ignore)]
        public string TableName;

        [JsonProperty("writeMode", NullValueHandling = NullValueHandling.Ignore)]
        public string WriteMode;

        [JsonProperty("useBulkInsert", NullValueHandling = NullValueHandling.Ignore)]
        public bool? UseBulkInsert;
    }
}
