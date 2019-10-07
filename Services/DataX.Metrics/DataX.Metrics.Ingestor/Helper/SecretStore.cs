// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Contract;
using DataX.ServiceHost.ServiceFabric;
using DataX.Utilities.KeyVault;
using System;
using System.Threading.Tasks;

namespace DataX.Metrics.Ingestor.Helper
{
    /// <summary>
    /// Singleton to manage all secrets retrieval.
    /// </summary>
    internal class SecretsStore
    {
        // make initialization thread-safe and lazy
        private static readonly Lazy<SecretsStore> _LazyInstance = new Lazy<SecretsStore>(() => new SecretsStore());
        private readonly string _keyVaultName;        

        private SecretsStore()
        {
            _keyVaultName = ServiceFabricUtil.GetServiceKeyVaultName().Result?.ToString();
        }

        public static SecretsStore Instance => _LazyInstance.Value;

        public async Task<string> GetMetricsEventHubListenerConnectionStringAsync()
        {
            return await GetSecretAsync(ServiceFabricUtil.GetServiceFabricConfigSetting("EventhubNamespaceConnectionstring"));
        }

        public async Task<string> GetMetricsStorageConnectionStringAsync()
        {
            return await GetSecretAsync(ServiceFabricUtil.GetServiceFabricConfigSetting("StorageAccountConnectionstring"));
        }
        
        public async Task<string> GetMetricsRedisConnectionStringAsync()
        {
            return await GetSecretAsync(ServiceFabricUtil.GetServiceFabricConfigSetting("RedisCacheConnectionstring"));
        }

        private async Task<string> GetSecretAsync(ApiResult setting)
        {
            string key = setting.Result?.ToString();

            KeyVaultManager keyManager = new KeyVaultManager();
            var secret = await keyManager.GetSecretStringAsync(_keyVaultName, key);
            return secret;
        }
    }
}
