// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Contract;
using DataX.Contract.Settings;
using DataX.Flow.Common.Models;
using DataX.ServiceHost;
using DataX.ServiceHost.ServiceFabric;
using DataX.Utilities.Blob;
using DataX.Utilities.CosmosDB;
using DataX.Utilities.KeyVault;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataX.Flow.Common
{
    /// <summary>
    /// Creating a common class where common functions and variables get set once you know the subscriptionId. Refactoring the services code to call into this common code.
    /// </summary>
    public class EngineEnvironment
    {
        private string _flowContainerName => EngineFlowConfig.FlowContainerName;

        public bool ResourceCreation = false;
        public string CosmosDBEndPoint { get; set; }
        public string CosmosDBUserName { get; set; }
        public string CosmosDBPassword { get; set; }
        public string CosmosDBDatabaseName { get; set; }
        public FlowConfigObject EngineFlowConfig { get; set; }
        public string FlowBlobConnectionString { get; set; }
        public string OpsBlobConnectionString { get; set; }
        public SparkConnectionInfo SparkConnInfo { get; set; }
        public string SparkPassword { get; set; }
        private readonly IConfiguration _configuration;

        /// <summary>
        /// OpsDiagnosticPath is the path that is used in c# code for calculating the garbage colection path for deleting the kernels
        /// </summary>
        public string OpsDiagnosticPath => $@"https://{EngineFlowConfig.OpsBlobBase}/diagnostics/";

        /// <summary>
        /// This is the product Config name
        /// </summary>
        public string ProductConfigName => $"{Name}-product.json";

        /// <summary>
        /// This is the path that is used in c# code for the sample data
        /// </summary>
        public string OpsSamplePath => $@"https://{EngineFlowConfig.OpsBlobBase}/samples";

        /// <summary>
        /// OpsSparkSamplePath is the path used in the scala code for intitialization of the kernel
        /// </summary>
        public string OpsSparkSamplePath => $@"wasbs://samples@{EngineFlowConfig.OpsBlobBase}/";

        public string Name { get; }
        public EngineEnvironment(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Moving the method that sets the various environment variables
        /// </summary>
        /// <returns>This returns the success or failure as understood by the frontend</returns>
        public async Task<ApiResult> GetEnvironmentVariables()
        {           
            CosmosDBDatabaseName = "production";
            var cosmosCon = "";
            var cosmosDBCollectionName = "";
            var response = new ApiResult();
            string serviceKeyvaultName = string.Empty;
            if (HostUtil.InServiceFabric)
            {
                response = ServiceFabricUtil.GetServiceKeyVaultName();
                if (response.Error.HasValue && response.Error.Value)
                {
                    return ApiResult.CreateError(response.Message);
                }
                serviceKeyvaultName = response.Result.ToString();
                cosmosCon = KeyVault.GetSecretFromKeyvault(ServiceFabricUtil.GetServiceFabricConfigSetting("cosmosDBConfigConnectionString").Result.ToString());
                CosmosDBDatabaseName = KeyVault.GetSecretFromKeyvault(ServiceFabricUtil.GetServiceFabricConfigSetting("cosmosDBConfigDatabaseName").Result.ToString());
                cosmosDBCollectionName = ServiceFabricUtil.GetServiceFabricConfigSetting("cosmosDBConfigCollectionName").Result.ToString();
            }
            else
            {
                serviceKeyvaultName = _configuration.GetSection(DataXSettingsConstants.ServiceEnvironment).GetSection(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_ServiceKeyVaultName).Value;
                cosmosCon = KeyVault.GetSecretFromKeyvault(_configuration.GetSection(DataXSettingsConstants.ServiceEnvironment).GetSection(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_CosmosDBConfigConnectionString).Value);
                CosmosDBDatabaseName = KeyVault.GetSecretFromKeyvault(_configuration.GetSection(DataXSettingsConstants.ServiceEnvironment).GetSection(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_CosmosDBConfigDatabaseName).Value);
                cosmosDBCollectionName = _configuration.GetSection(DataXSettingsConstants.ServiceEnvironment).GetSection(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_CosmosDBConfigCollectionName).Value;                
            }

            var namePassword = Helper.ParseCosmosDBUserNamePassword(cosmosCon);

            if (string.IsNullOrEmpty(cosmosCon) || string.IsNullOrEmpty(namePassword) || namePassword.Split(new char[] { ':' }).Count() != 2)
            {
                return ApiResult.CreateError("Can't get UserName or Password from CosmosDB connection string");
            }

            CosmosDBEndPoint = Helper.ParseCosmosDBEndPoint(cosmosCon);
            CosmosDBUserName = namePassword.Split(new char[] { ':' })[0];
            CosmosDBPassword = namePassword.Split(new char[] { ':' })[1];

            response = await CosmosDB.DownloadConfigFromDocumentDB(CosmosDBDatabaseName, CosmosDBEndPoint, CosmosDBUserName, CosmosDBPassword, cosmosDBCollectionName).ConfigureAwait(false);
            if (response.Error.HasValue && response.Error.Value)
            {
                return ApiResult.CreateError(response.Message);
            }

            var flowConfigObj = response.Result.ToObject<FlowConfigObject>();
            if (flowConfigObj != null)
            {
                EngineFlowConfig = flowConfigObj;
                ResourceCreation = flowConfigObj.ResourceCreation.ToLower().Equals("true") ? true : false;

                FlowBlobConnectionString = KeyVault.GetSecretFromKeyvault(serviceKeyvaultName, flowConfigObj.ConfiggenSecretPrefix + flowConfigObj.StorageAccountName + "-blobconnectionstring");
                OpsBlobConnectionString = KeyVault.GetSecretFromKeyvault(serviceKeyvaultName, flowConfigObj.ConfiggenSecretPrefix + flowConfigObj.OpsStorageAccountName + "-blobconnectionstring");
                if (EngineFlowConfig.SparkType != DataX.Config.ConfigDataModel.Constants.SparkTypeDataBricks)
                {
                    SparkConnInfo = Helper.ParseConnectionString(Helper.PathResolver(flowConfigObj.SparkConnectionString));
                }
                return ApiResult.CreateSuccess("");
            }    

            return ApiResult.CreateError("Failed to get environment variables");
        }

        /// <summary>
        /// This function is used to get the flowname in case the flow name is not passed in from the frontend. Once again removing this function from within the services and putting it in the common code
        /// </summary>
        /// <param name="subscriptionId">SubscriptionId is passed from the front end</param>
        /// <param name="displayName">DisplayName is the name of the flow as specified in the front end</param>
        /// <returns>Returns the Id/name of the flow</returns>
        public async Task<string> GetUniqueName(string subscriptionId, string displayName)
        {
            string name = null;

            if (!string.IsNullOrEmpty(subscriptionId))
            {
                var flowId = GenerateValidFlowId(displayName);
                name = flowId;

                var allConfigs = await GetAllConfigs(subscriptionId);

                var results = allConfigs.Result.ToObject<List<ProductConfigMetadata>>();

                int index = 1;
                while (results.Any(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    name = flowId + index++.ToString();
                }
            }
            return name;
        }

        public static string GenerateValidFlowId(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = Guid.NewGuid().ToString().Trim(new[] { '-', '{', '}' });
            }

            return Regex.Replace(displayName, "[^A-Za-z0-9]", "").ToLower();
        }

        /// <summary>
        /// This function gets called to get all the data from the configs within a subscription
        /// </summary>
        /// <param name="subscriptionId">The subscription Id</param>
        /// <returns>Returns the configMetadata JObject</returns>
        public async Task<ApiResult> GetAllConfigs(string subscriptionId)
        {
            var response = await GetEnvironmentVariables();
            if (response.Error.HasValue && response.Error.Value)
            {
                return ApiResult.CreateError(response.Message);
            }

            try
            {
                var configs = BlobStorage.LoadAllConfigsFromBlobStorage(subscriptionId, EngineFlowConfig.ServiceKeyVaultName, EngineFlowConfig.ResourceGroupName, EngineFlowConfig.ResourceGroupLocation, EngineFlowConfig.StorageAccountName, _flowContainerName, EngineFlowConfig.EnvironmentType, ProductConfigName, EngineFlowConfig.ConfiggenClientId, EngineFlowConfig.ConfiggenTenantId, EngineFlowConfig.ConfiggenSecretPrefix);

                List<ProductConfigMetadata> configMetadata = new List<ProductConfigMetadata>();

                foreach (var config in configs)
                {
                    ProductConfigMetadata value = new ProductConfigMetadata();
                    JObject productConfig = JObject.Parse(config);
                    value.DisplayName = productConfig.Value<string>("displayName");
                    value.Owner = productConfig.Value<string>("owner");
                    value.Name = productConfig.Value<string>("name");

                    configMetadata.Add(value);
                }

                return ApiResult.CreateSuccess(JArray.FromObject(configMetadata));
            }
            catch (Exception e)
            {
                return ApiResult.CreateError(e.Message);
            }
        }

        /// <summary>
        /// From: https://docs.microsoft.com/en-us/rest/api/storageservices/naming-and-referencing-containers--blobs--and-metadata
        /// Container Names
        /// A container name must be a valid DNS name, conforming to the following naming rules:
        /// Container names must start with a letter or number, and can contain only letters, numbers, and the dash(-) character.
        /// Every dash(-) character must be immediately preceded and followed by a letter or number; consecutive dashes are not permitted in container names.
        /// All letters in a container name must be lowercase.
        /// Container names must be from 3 through 63 characters long.
        /// </summary>
        /// <param name="name">Flow name as passed in from the frontend</param>
        /// <returns>returns the string abiding by the rules of an azure container name</returns>
        public string CheckPointContainerNameHelper(string name)
        {
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
            name = rgx.Replace(name, "");
            if (name.Length > 49)
            {
                name = name.Substring(0, 48);
            }
            return name;
        }

        /// <summary>
        /// Generate a unique name to be used as the KernelApplicationName for the kernel initializer
        /// </summary>
        /// <param name="flowId">flowId</param>
        /// <returns>Returns a unique name to be used as the KernelApplicationName for the kernel initializer</returns>
        public string GenerateKernelDisplayName(string flowId)
        {
            int min = 1000;
            int max = 9999;
            Random rdm = new Random();
            return $"LiveQuery-{flowId}-{rdm.Next(min, max)}";            
        }        
    }
}
