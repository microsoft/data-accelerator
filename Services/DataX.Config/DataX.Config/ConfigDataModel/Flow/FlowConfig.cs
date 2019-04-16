// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DataX.Config.Templating;
using DataX.Contract;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DataX.Config.ConfigDataModel
{
    /// <summary>
    /// Represents the flow config
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class FlowConfig : EntityConfig
    {
        public const string TokenName_Name = "name";
        public const string JsonFieldName_Gui = "gui";
        public const string JsonFieldName_JobNames = "jobNames";
        public const string JsonFieldName_Metrics = "metrics";

        protected FlowConfig()
        {
        }

        public static FlowConfig From(JsonConfig json)
        {
            return ConvertFrom<FlowConfig>(json);
        }

        public static FlowConfig From(string json)
        {
            return From(JsonConfig.From(json));
        }

        /// <summary>
        /// get the desitionation folder path for runtime config generation
        /// </summary>
        /// <returns>folder path</returns>
        public string GetJobConfigDestinationFolder()
        {
            var folder = CommonProcessor?.SparkJobConfigFolder;
            Ensure.NotNull(folder, "flow.commonProcessor.sparkJobConfigFolder");

            return folder;
        }

        /// <summary>
        /// Gets jobs' metadata to be created for this flow
        /// </summary>
        /// <returns>List of jobs under this flow</returns>
        public IList<JobMetadata> GetJobs(TokenDictionary commonTokens)
        {
            return this.CommonProcessor?.Jobs?.Select(job => new JobMetadata(job.ToTokens(), commonTokens)).ToList();
        }

        /// <summary>
        /// Gets the name of the spark job template for this flow
        /// </summary>
        /// <returns>Name of the spark job template</returns>
        public string GetSparkJobConfigTemplateName()
        {
            var result = CommonProcessor?.SparkJobTemplateRef;
            Ensure.NotNull(result, "flow.commonProcessor.sparkJobTemplateRef");

            return result;
        }

        public FlowGuiConfig GetGuiConfig()
        {
            return Gui?.ToObject<FlowGuiConfig>();
        }

        /// <summary>
        /// Merge with the given base config, taking the content of it as the default and overwrite them with the current content in this config
        /// </summary>
        /// <param name="baseConfig">the given base config to provide default values</param>
        /// <returns>A merged config</returns>
        public FlowConfig RebaseOn(FlowConfig baseConfig)
        {
            if (baseConfig == null)
            {
                return this;
            }
            else
            {
                return From(this.ToJson().RebaseOn(baseConfig.ToJson()));
            }
        }

        #region "Json Properties"
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("commonProcessor")]
        public FlowCommonProcessor CommonProcessor { get; set; }

        [JsonProperty(JsonFieldName_Metrics)]
        public MetricsConfig Metrics { get; set; }

        [JsonProperty(JsonFieldName_Gui)]
        public JToken Gui { get; set; }

        [JsonProperty("properties")]
        public Dictionary<string, string> Properties { get; set; }

        [JsonProperty(JsonFieldName_JobNames)]
        public string[] JobNames { get; set; }
        #endregion
    }
}
