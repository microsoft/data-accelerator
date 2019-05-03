// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Config.ConfigGeneration.Processor;
using DataX.Config.Utility;
using DataX.Contract;
using DataX.Utility.EventHub;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.Input.EventHub.Processor
{
    [Shared]
    [Export(typeof(IFlowDeploymentProcessor))]
    public class CreateEventHubConsumerGroup : ProcessorBase
    {
        public const string TokenName_InputEventHubConnectionString = "inputEventHubConnectionString";
        public const string TokenName_InputEventHubConsumerGroup = "inputEventHubConsumerGroup";
        public const string TokenName_InputEventHubs = "inputEventHubs";
        public const string TokenName_InputEventHubCheckpointDir = "inputEventHubCheckpointDir";
        public const string TokenName_InputEventHubCheckpointInterval = "inputEventHubCheckpointInterval";
        public const string TokenName_InputEventHubMaxRate = "inputEventHubMaxRate";
        public const string TokenName_InputEventHubFlushExistingCheckpoints = "inputEventHubFlushExistingCheckpoints";
        public const string TokenName_InputEventHubSubscriptionId = "inputEventHubSubscriptionId";
        public const string TokenName_InputEventHubResourceGroupName = "inputEventHubResourceGroupName";

        public const string ConfigSettingName_InputEventHubCheckpointDir = "inputEventHubCheckpointDir";
        public const string ConfigSettingName_InputEventHubFlushExistingCheckpoints = "inputEventHubFlushExistingCheckpoints";

        [ImportingConstructor]
        public CreateEventHubConsumerGroup(ConfigGenConfiguration configuration, IKeyVaultClient keyVaultClient)
        {
            Configuration = configuration;
            KeyVaultClient = keyVaultClient;
        }

        private ConfigGenConfiguration Configuration { get; }
        private IKeyVaultClient KeyVaultClient { get; }

        public override async Task<FlowGuiConfig> HandleSensitiveData(FlowGuiConfig guiConfig)
        {
            var runtimeKeyVaultName = Configuration[Constants.ConfigSettingName_RuntimeKeyVaultName];
            Ensure.NotNull(runtimeKeyVaultName, "runtimeKeyVaultName");

            // Replace Input Event Hub Connection String
            var eventHubConnectionString = guiConfig?.Input?.Properties?.InputEventhubConnection;
            if (eventHubConnectionString != null && !KeyVaultUri.IsSecretUri(eventHubConnectionString))
            {
                //TODO: create new secret
                var secretName = $"{guiConfig.Name}-input-eventhubconnectionstring";
                var secretId = await KeyVaultClient.SaveSecretAsync(runtimeKeyVaultName, secretName, eventHubConnectionString);
                guiConfig.Input.Properties.InputEventhubConnection = secretId;
            }

            // Replace Input Event Hub SubscriptionId
            var inputSubscriptionId = guiConfig?.Input?.Properties?.InputSubscriptionId;
            if (!string.IsNullOrEmpty(inputSubscriptionId) && !KeyVaultUri.IsSecretUri(inputSubscriptionId))
            {
                var secretName = $"{guiConfig.Name}-input-inputsubscriptionid";
                var secretId = await KeyVaultClient.SaveSecretAsync(runtimeKeyVaultName, secretName, inputSubscriptionId);
                guiConfig.Input.Properties.InputSubscriptionId = secretId;
            }

            // Replace Input Event Hub ResourceGroup
            var inputResourceGroup = guiConfig?.Input?.Properties?.InputResourceGroup;
            if (!string.IsNullOrEmpty(inputResourceGroup) && !KeyVaultUri.IsSecretUri(inputResourceGroup))
            {
                var secretName = $"{guiConfig.Name}-input-inputResourceGroup";
                var secretId = await KeyVaultClient.SaveSecretAsync(runtimeKeyVaultName, secretName, inputResourceGroup);
                guiConfig.Input.Properties.InputResourceGroup = secretId;
            }

            return guiConfig;
        }

        // Do input eventhub/iothub settings related processing
        public override async Task<string> Process(FlowDeploymentSession flowToDeploy)
        {
            var flowConfig = flowToDeploy.Config;
            var config = flowConfig?.GetGuiConfig()?.Input;

            var inputType = config?.InputType?.ToLowerInvariant();
            if (inputType == null)
            {
                return "eventhub/iothub input not defined, skipped";
            }

            if (inputType != Constants.InputType_EventHub && inputType != Constants.InputType_IoTHub && inputType != Constants.InputType_KafkaEventHub && inputType != Constants.InputType_Kafka)   
            {
                return $"unsupported inputtype '{inputType}', skipped.";
            }

            var props = config.Properties;
            Ensure.NotNull(props, "flowConfig.Gui.Input.Properties");

            var connectionString = props.InputEventhubConnection;
            flowToDeploy.SetStringToken(TokenName_InputEventHubConnectionString, connectionString);

            var resolvedConnectionString = await KeyVaultClient.ResolveSecretUriAsync(connectionString);
            var hubInfo = ConnectionStringParser.ParseEventHub(resolvedConnectionString);

            if (inputType == Constants.InputType_Kafka || inputType == Constants.InputType_KafkaEventHub)
            {
                hubInfo.Name = config.Properties.InputEventHubName;
            }

            var consumerGroupName = flowConfig.Name;

            // Create consumer group only if the resource creation flag is set. This is to support scenario where EventHub
            // is in a different subscription than where services are deployed
            if (Configuration[Constants.ConfigSettingName_ResourceCreation].ToLower(CultureInfo.InvariantCulture) == "true")
            {
                var serviceKeyVaultName = Configuration[Constants.ConfigSettingName_ServiceKeyVaultName];
                Ensure.NotNull(serviceKeyVaultName, "serviceKeyVaultName");
                var resolvedSecretKey = await KeyVaultClient.GetSecretFromKeyVaultAsync(serviceKeyVaultName, Configuration[Constants.ConfigSettingName_SecretPrefix] + "clientsecret");
                var clientId = Configuration[Constants.ConfigSettingName_ConfigGenClientId];
                var tenantId = Configuration[Constants.ConfigSettingName_ConfigGenTenantId];

                var inputSubscriptionId = string.IsNullOrEmpty(config?.Properties?.InputSubscriptionId) ? await KeyVaultClient.ResolveSecretUriAsync(flowToDeploy.GetTokenString(TokenName_InputEventHubSubscriptionId)) : await KeyVaultClient.ResolveSecretUriAsync(config?.Properties?.InputSubscriptionId);
                var inputResourceGroupName = string.IsNullOrEmpty(config?.Properties?.InputResourceGroup) ? flowToDeploy.GetTokenString(TokenName_InputEventHubResourceGroupName) : await KeyVaultClient.ResolveSecretUriAsync(config?.Properties?.InputResourceGroup);

                Result result = null;
                switch (inputType)
                {
                    case Constants.InputType_EventHub:
                    case Constants.InputType_Kafka:
                    case Constants.InputType_KafkaEventHub:
                        //Check for required parameters
                        if (string.IsNullOrEmpty(hubInfo.Namespace) || string.IsNullOrEmpty(hubInfo.Name))
                        {
                            throw new ConfigGenerationException("Could not parse Event Hub connection string; please check input.");
                        }
                        result = await EventHubUtil.CreateEventHubConsumerGroups(
                                                        clientId: clientId,
                                                        tenantId: tenantId,
                                                        secretKey: resolvedSecretKey,
                                                        subscriptionId: inputSubscriptionId,
                                                        resourceGroupName: inputResourceGroupName,
                                                        hubNamespace: hubInfo.Namespace,
                                                        hubNames: hubInfo.Name,
                                                        consumerGroupName: consumerGroupName);
                        break;
                    case Constants.InputType_IoTHub:
                        //Check for required parameters
                        if (string.IsNullOrEmpty(hubInfo.Name))
                        {
                            throw new ConfigGenerationException("Could not parse IoT Hub connection string; please check input.");
                        }
                        result = await EventHubUtil.CreateIotHubConsumerGroup(
                                                        clientId: clientId,
                                                        tenantId: tenantId,
                                                        secretKey: resolvedSecretKey,
                                                        subscriptionId: inputSubscriptionId,
                                                        resourceGroupName: inputResourceGroupName,
                                                        hubName: hubInfo.Name,
                                                        consumerGroupName: consumerGroupName);
                        break;
                    default:
                        throw new ConfigGenerationException($"unexpected inputtype '{inputType}'.");
                }

                Ensure.IsSuccessResult(result);
            }

            flowToDeploy.SetStringToken(TokenName_InputEventHubConsumerGroup, consumerGroupName);
            flowToDeploy.SetStringToken(TokenName_InputEventHubs, hubInfo.Name);

            var checkpointDir = Configuration.GetOrDefault(ConfigSettingName_InputEventHubCheckpointDir, "hdfs://mycluster/datax/direct/${name}/");
            flowToDeploy.SetStringToken(TokenName_InputEventHubCheckpointDir, checkpointDir);

            var intervalInSeconds = props?.WindowDuration;
            flowToDeploy.SetStringToken(TokenName_InputEventHubCheckpointInterval, intervalInSeconds);
            flowToDeploy.SetObjectToken(TokenName_InputEventHubMaxRate, props.MaxRate);

            var flushCheckpointsString = Configuration.GetOrDefault(ConfigSettingName_InputEventHubFlushExistingCheckpoints, "False");
            bool.TryParse(flushCheckpointsString, out bool flushExistingCheckpoints);
            flowToDeploy.SetObjectToken(TokenName_InputEventHubFlushExistingCheckpoints, flushExistingCheckpoints);

            return "done";
        }
    }
}
