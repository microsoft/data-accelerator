// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;

namespace DataX.Config.ConfigDataModel.RuntimeConfig
{
    public class FlowSqlOutputSpec
    {
        [JsonProperty("connectionStringRef")]
        public string ConnectionStringRef { get; set; }

        [JsonProperty("tableName")]
        public string TableName { get; set; }

        [JsonProperty("databaseName")]
        public string DatabaseName { get; set; }

        [JsonProperty("user")]
        public string User { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("writeMode")]
        public string WriteMode { get; set; }

        [JsonProperty("useBulkInsert")]
        public bool? UseBulkInsert { get; set; }
    }
}
