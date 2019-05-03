// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.Config.ConfigDataModel
{
    public static class Constants
    {
        public const string ConfigSettingName_RuntimeKeyVaultName = "sparkKeyVaultName";
        public const string ConfigSettingName_ServiceKeyVaultName= "serviceKeyVaultName";
        public const string ConfigSettingName_MetricEventHubConnectionKey = "metricEventHubConnectionStringKey";
        public const string ConfigSettingName_ConfigFolderHost = "cpConfigFolderBase";
        public const string ConfigSettingName_ConfigFolderContainerPath = "containerPath";
        public const string ConfigSettingName_ConfigGenSubscriptionId = "configgen-subscriptionid";
        public const string ConfigSettingName_ConfigGenClientId = "configgenClientId";
        public const string ConfigSettingName_ConfigGenTenantId = "configgenTenantId";
        public const string ConfigSettingName_ClusterName = "sparkClusterName";
        public const string ConfigSettingName_RuntimeApplicationInsightKey = "applicationInsightKey";
        public const string ConfigSettingName_EnableOneBox = "enableOneBox";
        public const string ConfigSettingName_LocalRoot = "localRoot";
        public const string ConfigSettingName_LocalMetricsHttpEndpoint = "localMetricsHttpEndpoint";
        public const string ConfigSettingName_SecretPrefix = "configgenSecretPrefix";
        public const string ConfigSettingName_ResourceCreation = "resourceCreation";

        public const string TokenName_SparkJobConfigFilePath = "sparkJobConfigFilePath";
        public const string TokenName_SparkJobName = "sparkJobName";

        public const string ConfigProcessorResultStatus_Completed = "done";

        public const string InputType_Kafka = "kafka";
        public const string InputType_IoTHub = "iothub";
        public const string InputType_EventHub = "events";
        public const string InputType_KafkaEventHub = "kafkaeventhub";
    }
}
