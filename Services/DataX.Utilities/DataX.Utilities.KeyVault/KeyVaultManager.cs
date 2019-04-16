// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using DataX.Contract;
using System;
using System.Threading.Tasks;

namespace DataX.Utilities.KeyVault
{
    /// <summary>
    /// This class uses Microsoft.KeyVault.Client library to call into Key Vault and retrieve a secret.
    /// Authentication when calling Key Vault is done through the configured X509 ceritifcate.
    /// </summary>
    public class KeyVaultManager : IDisposable
    {
        private KeyVaultClient _keyVaultClient;

        public KeyVaultManager()
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            _keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
        }

        /// <summary>
        /// Get a secret from Key Vault
        /// </summary>
        /// <param name="keyvaultName">ID of the secret</param>
        /// <returns>secret value</returns>
        public async Task<string> GetSecretStringAsync(string keyvaultName, string secretName)
        {
            try
            {
                var secretUrl = GetKeyVaultSecretUrl(keyvaultName) + $"/secrets/{secretName}";

                var secret = await _keyVaultClient.GetSecretAsync(secretUrl);
                return secret.Value;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Delete a secret from Key Vault
        /// </summary>
        /// <param name="keyvaultName">ID of the secret</param>
        /// <returns>secret value</returns>
        public async Task<ApiResult> GetAndDeleteSecretsAsync(string keyvaultName, string flowId)
        {
            try
            {
                var secretUrl = GetKeyVaultSecretUrl(keyvaultName);

                var secrets = await _keyVaultClient.GetSecretsAsync(secretUrl);
                foreach (Microsoft.Azure.KeyVault.Models.SecretItem secretItem in secrets)
                {
                    if (secretItem.Identifier.Name.StartsWith($"{flowId}-"))
                    {
                        await DeleteSecretAsync(keyvaultName, secretItem.Identifier.Name);
                    }
                }
                while (!string.IsNullOrWhiteSpace(secrets.NextPageLink))
                {
                    secrets = await _keyVaultClient.GetSecretsNextAsync(secrets.NextPageLink);
                    foreach (Microsoft.Azure.KeyVault.Models.SecretItem secretItem in secrets)
                    {
                        if (secretItem.Identifier.Name.StartsWith($"{flowId}-"))
                        {
                            await DeleteSecretAsync(keyvaultName, secretItem.Identifier.Name);
                        }
                    }
                }
                return ApiResult.CreateSuccess("Deleted");
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Delete a secret from Key Vault
        /// </summary>
        /// <param name="keyvaultName">ID of the secret</param>
        /// <returns>secret value</returns>
        public async Task<ApiResult> DeleteSecretAsync(string keyvaultName, string keyName)
        {
            try
            {
                var secretUrl = GetKeyVaultSecretUrl(keyvaultName);

                var secret = await _keyVaultClient.DeleteSecretAsync(secretUrl, keyName);
                return ApiResult.CreateSuccess("Deleted");
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="secretId"></param>
        /// <returns></returns>
        public async Task<string> SaveSecretStringAsync(string keyvaultName, string secretId, string value)
        {
            try
            {
                var secretUrlBase = GetKeyVaultSecretUrl(keyvaultName);
                var secret = await _keyVaultClient.SetSecretAsync(secretUrlBase, secretId, value);
                return secret.Value;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Get the url for the secret name
        /// </summary>
        /// <param name="secretName">Name of the secret being retrieved</param>
        /// <returns></returns>
        private string GetKeyVaultSecretUrl(string keyvaultName)
        {
            return $"https://{keyvaultName}.vault.azure.net:443";

        }

        /// <summary>
        /// Dispose object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _keyVaultClient.Dispose();
            }
        }
    }
}
