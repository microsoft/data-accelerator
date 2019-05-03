// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using DataX.Contract;
using DataX.Flow.Common;
using DataX.Flow.Common.Models;
using DataX.Utilities.Blob;
using DataX.Utilities.CosmosDB;
using DataX.Utilities.EventHub;
using DataX.Utilities.KeyVault;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DataX.Config.ConfigDataModel;

namespace DataX.Flow.DeleteHelper
{
    public class ConfigDeleter
    {
        private const string _Centralprocessing = "centralprocessing";
        private string _flowContainerName => _engineEnvironment.EngineFlowConfig.FlowContainerName;
        private string _eventHubPrimaryKeyListener;
        private EngineEnvironment _engineEnvironment = new EngineEnvironment();
        private readonly Dictionary<string, string> _keySecretList = new Dictionary<string, string>();

        private string ConfigName { get; set; }

        private List<string> _eventHubNames;
        private string _eventHubNamespace;
        private string _eventHubNameRole;
 
        private string ConsumerGroupName => ConfigName;

        private string ConsumerGroupNameDiagnostics => $"{ConfigName}-Diagnostic";

        private string CodeGenFileName => $"{ConfigName}-combined.txt";

        private string ProductConfigName => $"{ConfigName}-product.json";

        private string JobConfigName => $"{ConfigName}-job.json";

        private string ResourceConfigName => $"{ConfigName}-resources-changes.json";

        private string OutputTemplateFileName => $"outputTemplates-{ConfigName}.xml";

        private string OutputTemplatePath => $@"https://{_engineEnvironment.EngineFlowConfig.CPConfigFolderBase}/rules/outputTemplates";

        private string RuleDefinitionFileName => $"rules-{ConfigName}.json";

        private string RuleDefinitionPath => $@"https://{_engineEnvironment.EngineFlowConfig.CPConfigFolderBase}/rules/ruleDefinitions";

        private string FlowContainerPath => $"https://{_engineEnvironment.EngineFlowConfig.CPConfigFolderBase}/{_flowContainerName}/{_engineEnvironment.EngineFlowConfig.EnvironmentType}/{ConfigName}/{ProductConfigName}";

        public string CPConfigFolderPath => $"{_engineEnvironment.EngineFlowConfig.CPConfigFolderBase}/{_engineEnvironment.EngineFlowConfig.ContainerPath}/{ConfigName}";

        public string SparkJobConfigFolderPath => $"{_engineEnvironment.EngineFlowConfig.SparkJobConfigFolderBase}/{_engineEnvironment.EngineFlowConfig.ContainerPath}";

        private string ResourceConfigFolderPath => $"https://{_engineEnvironment.EngineFlowConfig.CPConfigFolderBase}/{_engineEnvironment.EngineFlowConfig.ResourceConfigPath}/{_engineEnvironment.EngineFlowConfig.ResourceConfigFileName}";

        private string _inputEventhubConnectionStringRef;
        private readonly ILogger _logger;

        public ConfigDeleter(ILogger logger)
        {
            _logger = logger;
        }

        /// This is the function that can be called for deleting all assets created on save of a flow: consumergroup, secrets in Key Vault, cosmosDB products document such that this flow stops showing up in the UI under Flows and blobs
        /// Please note if a a new asset is created on Azure as part of saving the flow or any other action taken by user in the UI, this function will need to be updated so that the asset can be deleted on delete of the flow.
        /// </summary>
        /// <param name="jObject">jObject requires: Subscription; Name; EventhubConnectionString; IsIotHub;EventhubName;userID</param>
        /// <returns>Returns result - success or failure as the case maybe</returns>
        public async Task<ApiResult> DeleteFlow(JObject jObject)
        {
            var diag = jObject.ToObject<InteractiveQueryObject>();
         
            ConfigName = diag.Name;
            bool errorExists = false;
            var response = await _engineEnvironment.GetEnvironmentVariables();
            if (response.Error.HasValue && response.Error.Value)
            {
                _logger.LogError(response.Message);
                return ApiResult.CreateError(response.Message);
            }

            ///Delete consumer group
            _logger.LogInformation($"For FlowId: {ConfigName} Deleting flow specific consumer group.. ");
            var inputEventhubConnection = Helper.GetSecretFromKeyvaultIfNeeded(diag.EventhubConnectionString);

            _inputEventhubConnectionStringRef = Helper.IsKeyVault(diag.EventhubConnectionString) ? diag.EventhubConnectionString : Helper.GenerateNewSecret(_keySecretList, _engineEnvironment.EngineFlowConfig.SparkKeyVaultName, ConfigName + "-input-eventhubconnectionstring", diag.EventhubConnectionString, false);
            diag.EventhubConnectionString = _inputEventhubConnectionStringRef;

            if (diag.InputType == Constants.InputType_EventHub)
            { 
                var ehName = Helper.ParseEventHub(inputEventhubConnection);
                _eventHubNamespace = Helper.ParseEventHubNamespace(inputEventhubConnection);
                _eventHubNameRole = Helper.ParseEventHubPolicyName(inputEventhubConnection);
                _eventHubPrimaryKeyListener = Helper.ParseEventHubAccessKey(inputEventhubConnection);

                if (string.IsNullOrWhiteSpace(ehName) || string.IsNullOrWhiteSpace(_eventHubNamespace) || string.IsNullOrWhiteSpace(_eventHubNameRole) || string.IsNullOrWhiteSpace(_eventHubPrimaryKeyListener))
                {
                    string error = "The connection string for Event Hub input type must contain Endpoint, SharedAccessKeyName, SharedAccessKey, and EntityPath";
                    _logger.LogError(error);
                    errorExists = true;
                }

                _eventHubNames = new List<string>() { ehName };
            }
            else
            {
                _eventHubNames = Helper.ParseEventHubNames(diag.EventhubNames);
                _eventHubNamespace = Helper.ParseEventHubNamespace(inputEventhubConnection);
                _eventHubNameRole = Helper.ParseEventHubPolicyName(inputEventhubConnection);
                _eventHubPrimaryKeyListener = Helper.ParseEventHubAccessKey(inputEventhubConnection);

                if (_eventHubNames.Count < 1)
                {
                    string error = "The event hub-compatible name for IoT Hub input type must be defined";
                    _logger.LogError(error);
                    errorExists = true;
                }

                if (string.IsNullOrWhiteSpace(_eventHubNamespace) || string.IsNullOrWhiteSpace(_eventHubNameRole) || string.IsNullOrWhiteSpace(_eventHubPrimaryKeyListener))
                {
                    string error = "The event hub-compatible endpoint for IoT Hub input type must contain Endpoint, SharedAccessKeyName, and SharedAccessKey";
                    _logger.LogError(error);
                    errorExists = true;
                }
            }

            // ResourceCreation is one of the environment variables.
            // If you don't want to create resource, you can set this to false.
            if (_engineEnvironment.ResourceCreation)
            {
                var inputSubscriptionId = string.IsNullOrEmpty(diag.InputSubscriptionId) ? Helper.GetSecretFromKeyvaultIfNeeded(_engineEnvironment.EngineFlowConfig.SubscriptionId) : Helper.GetSecretFromKeyvaultIfNeeded(diag.InputSubscriptionId);
                var inputResourceGroup = string.IsNullOrEmpty(diag.InputResourceGroup) ? _engineEnvironment.EngineFlowConfig.EventHubResourceGroupName : Helper.GetSecretFromKeyvaultIfNeeded(diag.InputResourceGroup);

                foreach (string ehName in _eventHubNames)
                {
                    var result = EventHub.DeleteConsumerGroup(inputSubscriptionId, _engineEnvironment.EngineFlowConfig.ServiceKeyVaultName, inputResourceGroup, _engineEnvironment.EngineFlowConfig.EventHubResourceGroupLocation, _eventHubNamespace, ehName, ConsumerGroupName, diag.InputType, _engineEnvironment.EngineFlowConfig.ConfiggenClientId, _engineEnvironment.EngineFlowConfig.ConfiggenTenantId, _engineEnvironment.EngineFlowConfig.ConfiggenSecretPrefix);
                    if (result.Error.HasValue && result.Error.Value)
                    {
                        _logger.LogError(result.Message);
                        errorExists = true;
                    }
                    else
                    {
                        _logger.LogInformation($"For FlowId: {ConfigName} Successfully deleted flow specific consumer group");
                    }
                }
            }


            ///Delete cosmosDB document related to a flow
            response = await CosmosDB.DeleteConfigFromDocumentDB(_engineEnvironment.CosmosDBDatabaseName, _engineEnvironment.CosmosDBEndPoint, _engineEnvironment.CosmosDBUserName, _engineEnvironment.CosmosDBPassword, "flows", ConfigName);
            if (response.Error.HasValue && response.Error.Value)
            {
                _logger.LogError(response.Message);
                errorExists = true;
            }
            else
            {
                _logger.LogInformation($"For FlowId: {ConfigName} Successfully Deleted flow specific cosmosDB entry");
            }

            ///Delete configs stored in blobs
            // ruleDefinitions
            response = await BlobHelper.DeleteBlob(_engineEnvironment.FlowBlobConnectionString, Path.Combine(RuleDefinitionPath, RuleDefinitionFileName));
            if (response.Error.HasValue && response.Error.Value)
            {
                _logger.LogError(response.Message);
                errorExists = true;
            }
            else
            {
                _logger.LogInformation($"For FlowId: {ConfigName} Successfully Deleted flow specific rules definition blob");
            }

            // outputTemplates
            response = await BlobHelper.DeleteBlob(_engineEnvironment.FlowBlobConnectionString, Path.Combine(OutputTemplatePath, OutputTemplateFileName));
            if (response.Error.HasValue && response.Error.Value)
            {
                _logger.LogError(response.Message);
                errorExists = true;
            }
            else
            {
                _logger.LogInformation($"For FlowId: {ConfigName} Successfully Deleted flow specific output template blob");
            }

            string resourceGroupLocation = _engineEnvironment.EngineFlowConfig.ResourceGroupLocation;
            string resourceGroupName = _engineEnvironment.EngineFlowConfig.ResourceGroupName;
            string storageAccountName = _engineEnvironment.EngineFlowConfig.StorageAccountName;
            string containerPath = Path.Combine(_flowContainerName, _engineEnvironment.EngineFlowConfig.EnvironmentType, ConfigName);
            string subscriptionId = Helper.GetSecretFromKeyvaultIfNeeded(_engineEnvironment.EngineFlowConfig.SubscriptionId);

            BlobStorage.DeleteAllConfigsFromBlobStorage(subscriptionId, _engineEnvironment.EngineFlowConfig.ServiceKeyVaultName, resourceGroupName, resourceGroupLocation, storageAccountName, _Centralprocessing, Path.Combine(_engineEnvironment.EngineFlowConfig.ContainerPath, ConfigName), _engineEnvironment.EngineFlowConfig.ConfiggenClientId, _engineEnvironment.EngineFlowConfig.ConfiggenTenantId, _engineEnvironment.EngineFlowConfig.ConfiggenSecretPrefix);
            _logger.LogInformation($"For FlowId: {ConfigName} Successfully Deleted flow specific blobs under the folder {ConfigName} under container {_Centralprocessing}");

            BlobStorage.DeleteAllConfigsFromBlobStorage(subscriptionId, _engineEnvironment.EngineFlowConfig.ServiceKeyVaultName, resourceGroupName, resourceGroupLocation, storageAccountName, _Centralprocessing, Path.Combine(_engineEnvironment.EngineFlowConfig.ContainerPath, ConfigName), _engineEnvironment.EngineFlowConfig.ConfiggenClientId, _engineEnvironment.EngineFlowConfig.ConfiggenTenantId, _engineEnvironment.EngineFlowConfig.ConfiggenSecretPrefix);

            BlobStorage.DeleteAllConfigsFromBlobStorage(subscriptionId, _engineEnvironment.EngineFlowConfig.ServiceKeyVaultName, resourceGroupName, resourceGroupLocation, storageAccountName, _flowContainerName, Path.Combine(_engineEnvironment.EngineFlowConfig.EnvironmentType, ConfigName), _engineEnvironment.EngineFlowConfig.ConfiggenClientId, _engineEnvironment.EngineFlowConfig.ConfiggenTenantId, _engineEnvironment.EngineFlowConfig.ConfiggenSecretPrefix);

            _logger.LogInformation($"For FlowId: {ConfigName} Successfully Deleted flow specific productconfig: {ProductConfigName} and {JobConfigName} for {ConfigName}.");

            /// Delete sample data and the checkpoints folder if it exists for that flow
            var hashValue = Helper.GetHashCode(diag.UserName);
            await BlobHelper.DeleteBlob(_engineEnvironment.OpsBlobConnectionString, Path.Combine(_engineEnvironment.OpsSamplePath, $"{ConfigName}-{hashValue}.json"));
            _logger.LogInformation($"For FlowId: {ConfigName} Successfully Deleted flow specific sampledata file: {ConfigName}-{ hashValue}.json");

            await BlobHelper.DeleteAllBlobsInAContainer(_engineEnvironment.OpsBlobConnectionString, $"{_engineEnvironment.CheckPointContainerNameHelper(ConfigName)}-checkpoints", _engineEnvironment.EngineFlowConfig.OpsBlobDirectory);
            _logger.LogInformation($"For FlowId: {ConfigName} Successfully Deleted flow specific checkpoints for {ConfigName}.");

            _logger.LogInformation("Deleting flow specific secrets..");
            ///Delete secrets specific to a flow from KeyVault
            KeyVault.GetSecretsAndDeleteFromKeyvault(_engineEnvironment.EngineFlowConfig.SparkKeyVaultName, ConfigName);
            _logger.LogInformation($"For FlowId: {ConfigName} Successfully Deleted flow specific secrets");

            if (!errorExists)
            {
                return ApiResult.CreateSuccess("Deleted!");
            }
            else
            {
                return ApiResult.CreateError("Deleted but with some error. Please check logs for details");
            }
        }

     }
}
