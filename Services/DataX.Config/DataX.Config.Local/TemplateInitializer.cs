// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Contract;
using System;
using System.IO;
using System.Composition;
using System.Threading.Tasks;

namespace DataX.Config.Local
{
    /// <summary>
    /// Add default config template data to designTime storage for local scenarios
    /// </summary>
    [Shared]
    [Export]
    public class TemplateInitializer
    {
        private const string _DefaultFlowTemplate="localFlowTemplate.json";
        private const string _DefaultSparkJobTemplate = "localSparkJobTemplate.json";
        private const string _DefaultSparkClusterConfig="localSparkCluster.json";
        private const string _FlattenerConfig = "flattenerConfig.json";
        private const string _SparkJobTemplateName = "defaultSparkJobTemplate";
        private const string _SamplesFolder = "samples";

        [ImportingConstructor]
        public TemplateInitializer(
            ConfigGenConfiguration conf,
            ICommonDataManager commonData,
            IDesignTimeConfigStorage designTimeStorage)
        {            
            Configuration = conf;
            CommonData = commonData;
            DesignTimeStorage = designTimeStorage;
        }
        
        private ConfigGenConfiguration Configuration { get; }
        private ICommonDataManager CommonData { get; }
        private IDesignTimeConfigStorage DesignTimeStorage { get; }
         
        public async Task<Result> Initialize()
        {
            try
            {
                // Load common templates
                await AddTemplateFromResourceFile(_DefaultFlowTemplate, FlowDataManager.CommonDataName_DefaultFlowConfig);
                await AddTemplateFromResourceFile(_DefaultSparkJobTemplate, _SparkJobTemplateName);
                await AddTemplateFromResourceFile(_FlattenerConfig, ConfigFlattenerManager.DefaultConfigName);

                // Load cluster template
                var localClusterContent = ResourceUtility.GetEmbeddedResource(this.GetType(), _DefaultSparkClusterConfig);
                await DesignTimeStorage.SaveByName("localCluster", localClusterContent, SparkClusterData.DataCollectionName);

                // Load samples
                await LoadSamples();

                return new SuccessResult("Templates initialized for local");
            }
            catch (Exception ex)
            {
                return new FailedResult($"Templates failed to initialized for local with error {ex.Message}");
            }
        }

        private async Task<Result> AddTemplateFromResourceFile(string resourceFileName, string templateName)
        {
            var content = ResourceUtility.GetEmbeddedResource(this.GetType(), resourceFileName);
            var result = await InsertTemplateToCommonData(templateName, content);
            Ensure.IsSuccessResult(result);
            return result;
        }

        private async Task<Result> InsertTemplateToCommonData(string name, string content)
        {
            var existing = await CommonData.GetByName(name);
            if (existing == null)
            {                
                var result = await CommonData.SaveByName(name, JsonConfig.From(content));
                return result;
            }
            else
            {
                //skip if it already exists
                return new SuccessResult($"Already exists, skipped");
            }
        }

        // Load the flow configs for the local samples to the local store
        private async Task<Result> LoadSamples()
        {
            if(Directory.Exists(_SamplesFolder)) 
            {
                 foreach(string fileName in Directory.GetFiles(_SamplesFolder, "*.json"))
                 {
                    string content = File.ReadAllText(fileName);
                    await DesignTimeStorage.SaveByName(fileName, content, FlowDataManager.DataCollectionName).ConfigureAwait(false);
                 }
            }
            return new SuccessResult("Samples loaded");
        }
    }
}
