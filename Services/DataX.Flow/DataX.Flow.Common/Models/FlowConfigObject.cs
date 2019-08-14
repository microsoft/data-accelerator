// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Flow.Common.Models
{
    public class FlowConfigObject
    {
        [JsonProperty("resourceCreation")]
        public string ResourceCreation;

        [JsonProperty("resourceGroupName")]
        public string ResourceGroupName;

        [JsonProperty("resourceGroupLocation")]
        public string ResourceGroupLocation;

        [JsonProperty("eventHubResourceGroupName")]
        public string EventHubResourceGroupName;

        [JsonProperty("eventHubResourceGroupLocation")]
        public string EventHubResourceGroupLocation;

        [JsonProperty("storageAccountName")]
        public string StorageAccountName;

        [JsonProperty("opsStorageAccountName")]
        public string OpsStorageAccountName;

        [JsonProperty("environmentType")]
        public string EnvironmentType;

        [JsonProperty("containerPath")]
        public string ContainerPath;
                    
        [JsonProperty("resourceConfigPath")]
        public string ResourceConfigPath;
                    
        [JsonProperty("cpConfigFolderBase")]
        public string CPConfigFolderBase;

        [JsonProperty("opsBlobBase")]
        public string OpsBlobBase;

        [JsonProperty("sparkJobConfigFolderBase")]
        public string SparkJobConfigFolderBase;
        
        [JsonProperty("resourceConfigFileName")]
        public string ResourceConfigFileName;

        [JsonProperty("sparkJobTemplateRef")]
        public string SparkJobTemplateRef;

        [JsonProperty("serviceKeyVaultName")]
        public string ServiceKeyVaultName;

        [JsonProperty("sparkKeyVaultName")]
        public string SparkKeyVaultName;

        [JsonProperty("jobURLBase")]
        public string JobURLBase;

        [JsonProperty("binaryName")]
        public JArray BinaryName;

        [JsonProperty("configgenClientId")]
        public string ConfiggenClientId;

        [JsonProperty("configgenClientSecret")]
        public string ConfiggenClientSecret;

        [JsonProperty("configgenClientResourceId")]
        public string ConfiggenClientResourceId;

        [JsonProperty("configgenTenantId")]
        public string ConfiggenTenantId;

        [JsonProperty("flowContainerName")]
        public string FlowContainerName;

        [JsonProperty("consumerGroup")]
        public string ConsumerGroup;

        [JsonProperty("opsBlobDirectory")]
        public string OpsBlobDirectory;

        [JsonProperty("configgenSecretPrefix")]
        public string ConfiggenSecretPrefix;

        [JsonProperty("interactiveQueryDefaultContainer")]
        public string InteractiveQueryDefaultContainer;

        [JsonProperty("designTimeCosmosDbConnectionString")]
        public string DesignTimeCosmosDbConnectionString;

        [JsonProperty("designTimeCosmosDbDatabaseName")]
        public string DesignTimeCosmosDbDatabaseName;

        [JsonProperty("sparkClusterName")]
        public string SparkClusterName;

        [JsonProperty("subscriptionId")]
        public string SubscriptionId;

        [JsonProperty("sparkConnectionString")]
        public string SparkConnectionString;

        [JsonProperty("sparkType")]
        public string SparkType;

        [JsonProperty("sparkUserToken")]
        public string SparkUserToken;

        [JsonProperty("sparkRegion")]
        public string SparkRegion;
    }
}
