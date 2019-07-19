// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;

namespace DataX.Config.ConfigDataModel
{
    [JsonObject(MemberSerialization.OptIn)]
    public class FlowGuiConfig : EntityConfig
    {
        protected FlowGuiConfig()
        {
        }

        public static FlowGuiConfig From(JsonConfig json)
        {
            return ConvertFrom<FlowGuiConfig>(json);
        }

        public static FlowGuiConfig From(string json)
        {
            return From(JsonConfig.From(json));
        }

        #region "Json Properties"
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
        [JsonProperty("databricksToken")]
        public string DatabricksToken { get; set; }
        [JsonProperty("owner")]
        public string Owner { get; set; }
        [JsonProperty("input")]
        public FlowGuiInput Input { get; set; }
        [JsonProperty("process")]
        public FlowGuiProcess Process { get; set; }
        [JsonProperty("outputs")]
        public FlowGuiOutput[] Outputs { get; set; }
        [JsonProperty("outputTemplates")]
        public FlowGuiOutputTemplate[] OutputTemplates { get; set; }
        [JsonProperty("rules")]
        public FlowGuiRule[] Rules { get; set; }
        [JsonProperty("batchList")]
        public FlowGuiInputBatchJob[] BatchList { get; set; }
        [JsonProperty("subscription")]
        public string Subscription { get; set; }
        #endregion
    }
}
