// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;

namespace DataX.Flow.Common.Models
{
    public class ProductConfigMetadata
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("displayName")]
        public string DisplayName;

        [JsonProperty("owner")]
        public string Owner;
    }
    public class ReferenceDataObject
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("properties")]
        public ReferenceDataObjectProperties Properties;

        [JsonProperty("typeDisplay")]
        public string TypeDisplay;
    }
    public class ReferenceDataObjectProperties
    {
        [JsonProperty("path")]
        public string Path;

        [JsonProperty("delimiter")]
        public string Delimiter;

        [JsonProperty("header")]
        public bool Header;
    }
}
