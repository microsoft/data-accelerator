// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json.Linq;
using DataX.Contract;
using System;
using System.Threading.Tasks;

namespace DataX.Utilities.KeyVault
{
    public static class KeyVaultUtility
    {
        public static async Task<string> GetSecretFromKeyvault(string keyvaultName, string name)
        {
            var result = await GetSecretFromKeyVaultAsync(keyvaultName, name);
            if (result.Error.HasValue && result.Error.Value == true)
            {
                throw new InvalidOperationException("Unable to get keysecret. " + result.Message);
            }

            var authKey = (string)result.Result;

            return authKey;
        }

        /// <summary>
        /// GetSecretsAndDeleteSecretsFromKeyVault
        /// </summary>
        /// <param name="keyvaultName">keyvaultName</param>
        /// <param name="flowId">flowId</param>
        /// <returns>ApiResult which contains error or successful result as the case may be</returns>
        public static async Task<string> GetSecretsAndDeleteSecretsFromKeyVault(string keyvaultName, string flowId)
        {
            var result = await GetSecretsAndDeleteSecretsFromKeyVaultAsync(keyvaultName, flowId);
            if (result.Error.HasValue && result.Error.Value == true)
            {
                throw new InvalidOperationException("the key does not exist: " + result.Message);
            }

            return "Successfully Deleted";
        }

        public static async Task<string> SaveSecretToKeyvault(string keyvaultName, string name, string value)
        {
            var result = await SaveSecretToKeyValutAsync(keyvaultName, name, value);
            if (result.Error.HasValue && result.Error.Value == true)
            {
                throw new InvalidOperationException("Unable to set keysecret. " + result.Message);
            }

            var authKey = (string)result.Result;

            return authKey;
        }

        private static async Task<ApiResult> GetSecretFromKeyVaultAsync(string keyvaultName, string name)
        {
            KeyVaultManager keyManager = new KeyVaultManager();

            try
            {
                var secret = await keyManager.GetSecretStringAsync(keyvaultName, name);
                return ApiResult.CreateSuccess(JToken.FromObject(secret));
            }
            catch (Exception ex)
            {
                return ApiResult.CreateError(ex.Message);
            }
        }

        /// <summary>
        /// GetSecretsAndDeleteSecretsFromKeyVaultAsync
        /// </summary>
        /// <param name="keyvaultName">keyvaultName</param>
        /// <param name="flowId">flowId</param>
        /// <returns>ApiResult which contains error or successful result as the case may be</returns>
        private static async Task<ApiResult> GetSecretsAndDeleteSecretsFromKeyVaultAsync(string keyvaultName, string flowId)
        {
            KeyVaultManager keyManager = new KeyVaultManager();

            try
            {
                var response = await keyManager.GetAndDeleteSecretsAsync(keyvaultName, flowId);
                return ApiResult.CreateSuccess(JToken.FromObject(response));
            }
            catch (Exception ex)
            {
                return ApiResult.CreateError(ex.Message);
            }
        }

        private static async Task<ApiResult> SaveSecretToKeyValutAsync(string keyvaultName, string name, string value)
        {
            KeyVaultManager keyVaultManager = new KeyVaultManager();

            try
            {
                var secret = await keyVaultManager.SaveSecretStringAsync(keyvaultName, name, value);
                return ApiResult.CreateSuccess(JToken.FromObject(secret));
            }
            catch (Exception ex)
            {
                return ApiResult.CreateError(ex.Message);
            }
        }
    }
}
