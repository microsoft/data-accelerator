// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using DataX.Contract;
using DataX.Flow.Common;
using DataX.Flow.Common.Models;
using DataX.Utilities.EventHub;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DataX.Flow.SchemaInference
{
    public class SchemaInferenceManager
    {
        private EngineEnvironment _engineEnvironment = new EngineEnvironment();
        private readonly ILogger _logger;

        private string OpsSamplePath => $@"https://{_engineEnvironment.EngineFlowConfig.OpsBlobBase}/samples";

        public SchemaInferenceManager(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<ApiResult> GetInputSchema(JObject jObject)
        {
            var diag = jObject.ToObject<InteractiveQueryObject>();

            if(_engineEnvironment.EngineFlowConfig == null)
            {
                await _engineEnvironment.GetEnvironmentVariables();
            }

            if (string.IsNullOrEmpty(diag.Name))
            {
                diag.Name = await _engineEnvironment.GetUniqueName(Helper.GetSecretFromKeyvaultIfNeeded(_engineEnvironment.EngineFlowConfig.SubscriptionId), diag.DisplayName);
            }

            var connectionString = Helper.GetSecretFromKeyvaultIfNeeded(diag.EventhubConnectionString);

            if (diag.InputType=="Event")
            {
                string ehName = Helper.ParseEventHub(connectionString);
                diag.EventhubNames = new List<string>() { ehName };
            }

            string eventHubNamespace = Helper.ParseEventHubNamespace(connectionString);

            //Fixing a bug where '_' checkpoint container name cannot have '_'
            var checkPointContainerName = $"{_engineEnvironment.CheckPointContainerNameHelper(diag.Name)}-checkpoints";

            // ResourceCreation is one of the environment variables.
            // If you don't want to create resource, you can set this to false.
            if (_engineEnvironment.ResourceCreation)
            {
                var inputSubscriptionId = string.IsNullOrEmpty(diag.InputSubscriptionId) ? Helper.GetSecretFromKeyvaultIfNeeded(_engineEnvironment.EngineFlowConfig.SubscriptionId) : Helper.GetSecretFromKeyvaultIfNeeded(diag.InputSubscriptionId);
                var inputResourceGroup = string.IsNullOrEmpty(diag.InputResourceGroup) ? _engineEnvironment.EngineFlowConfig.EventHubResourceGroupName : Helper.GetSecretFromKeyvaultIfNeeded(diag.InputResourceGroup);

                // Create consumer group if it doesn't exist
                foreach (string ehName in diag.EventhubNames)
                {
                    var result = EventHub.CreateConsumerGroup(inputSubscriptionId, _engineEnvironment.EngineFlowConfig.ServiceKeyVaultName, inputResourceGroup, _engineEnvironment.EngineFlowConfig.EventHubResourceGroupLocation, eventHubNamespace, ehName, _engineEnvironment.EngineFlowConfig.ConsumerGroup, diag.InputType, _engineEnvironment.EngineFlowConfig.ConfiggenClientId, _engineEnvironment.EngineFlowConfig.ConfiggenTenantId, _engineEnvironment.EngineFlowConfig.ConfiggenSecretPrefix);
                    if (result.Error.HasValue && result.Error.Value)
                    {
                        _logger.LogError(result.Message);
                        return ApiResult.CreateError(result.Message);
                    }
                }
            }

            // Sample events and generate schema
            SchemaGenerator sg = new SchemaGenerator(diag.EventhubNames, _engineEnvironment.EngineFlowConfig.ConsumerGroup, connectionString, _engineEnvironment.OpsBlobConnectionString, checkPointContainerName, _engineEnvironment.EngineFlowConfig.OpsBlobDirectory, diag.InputType, _logger);
            SchemaResult schema = await sg.GetSchemaAsync(_engineEnvironment.OpsBlobConnectionString, OpsSamplePath, diag.UserName, diag.Name, diag.Seconds);
                
                return ApiResult.CreateSuccess(JObject.FromObject(schema));
        }

        public async Task<ApiResult> RefreshSample(JObject jObject)
        {
            var diag = jObject.ToObject<InteractiveQueryObject>();

            if (_engineEnvironment.EngineFlowConfig == null)
            {
                await _engineEnvironment.GetEnvironmentVariables();
            }

            var connectionString = Helper.GetSecretFromKeyvaultIfNeeded(diag.EventhubConnectionString);

            if (diag.InputType == "Event")
            {
                string ehName = Helper.ParseEventHub(connectionString);
                diag.EventhubNames = new List<string>() { ehName };
            }

            string eventHubNamespace = Helper.ParseEventHubNamespace(connectionString);

            // ResourceCreation is one of the environment variables.
            // If you don't want to create resource, you can set this to false.
            if (_engineEnvironment.ResourceCreation)
            {
                var inputSubscriptionId = string.IsNullOrEmpty(diag.InputSubscriptionId) ? Helper.GetSecretFromKeyvaultIfNeeded(_engineEnvironment.EngineFlowConfig.SubscriptionId) : Helper.GetSecretFromKeyvaultIfNeeded(diag.InputSubscriptionId);
                var inputResourceGroup = string.IsNullOrEmpty(diag.InputResourceGroup) ? _engineEnvironment.EngineFlowConfig.EventHubResourceGroupName : Helper.GetSecretFromKeyvaultIfNeeded(diag.InputResourceGroup);

                // Create consumer group if it doesn't exist
                foreach (string ehName in diag.EventhubNames)
                {
                    var result = EventHub.CreateConsumerGroup(inputSubscriptionId, _engineEnvironment.EngineFlowConfig.ServiceKeyVaultName, inputResourceGroup, _engineEnvironment.EngineFlowConfig.EventHubResourceGroupLocation, eventHubNamespace, ehName, _engineEnvironment.EngineFlowConfig.ConsumerGroup, diag.InputType, _engineEnvironment.EngineFlowConfig.ConfiggenClientId, _engineEnvironment.EngineFlowConfig.ConfiggenTenantId, _engineEnvironment.EngineFlowConfig.ConfiggenSecretPrefix);
                    if (result.Error.HasValue && result.Error.Value)
                    {
                        _logger.LogError(result.Message);
                        return ApiResult.CreateError(result.Message);
                    }
                }
            }

            if (string.IsNullOrEmpty(diag.Name))
            {
                diag.Name = await _engineEnvironment.GetUniqueName(Helper.GetSecretFromKeyvaultIfNeeded(_engineEnvironment.EngineFlowConfig.SubscriptionId), diag.DisplayName);
            }

            //Fixing a bug where '_' checkpoint container name cannot have '_'
            var checkPointContainerName = $"{_engineEnvironment.CheckPointContainerNameHelper(diag.Name)}-checkpoints";

            // Sample events and refresh sample
            SchemaGenerator sg = new SchemaGenerator(diag.EventhubNames, _engineEnvironment.EngineFlowConfig.ConsumerGroup, connectionString, _engineEnvironment.OpsBlobConnectionString, checkPointContainerName, _engineEnvironment.EngineFlowConfig.OpsBlobDirectory, diag.InputType, _logger);
            await sg.RefreshSample(_engineEnvironment.OpsBlobConnectionString, OpsSamplePath, diag.UserName, diag.Name, diag.Seconds);
            _logger.LogInformation("Refresh Sample worked!");
            return ApiResult.CreateSuccess("success");
        }
    }
}
