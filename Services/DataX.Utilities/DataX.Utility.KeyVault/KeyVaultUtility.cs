// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Services.AppAuthentication;
using DataX.Contract.Exception;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataX.Utility.KeyVault
{
    /// <summary>
    /// This class uses Microsoft.KeyVault.Client library to call into Key Vault and retrieve a secret.
    /// Authentication when calling Key Vault is done through the configured MSI.
    /// </summary>
    public class KeyVaultUtility : IDisposable
    {
        private KeyVaultClient _keyVaultClient;

        public KeyVaultUtility()
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
            var secretUrl = GetKeyVaultSecretUrl(keyvaultName) + $"/secrets/{secretName}";
            try
            {
                var secret = await _keyVaultClient.GetSecretAsync(secretUrl);
                return secret.Value;
            }
            catch(KeyVaultErrorException kve)
            {
                throw new GeneralException($"Hit KeyVault Error while getting secret '{secretName}' from keyvault '{keyvaultName}': {kve.Message}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="secretName"></param>
        /// <returns></returns>
        public async Task<string> SaveSecretStringAsync(string keyvaultName, string secretName, string value)
        {
            var secretUrlBase = GetKeyVaultSecretUrl(keyvaultName);
            //TODO: we may want to add some retries here in case network hipcups
            var secret = await _keyVaultClient.SetSecretAsync(secretUrlBase, secretName, value);
            return secret.Value;
        }

        /// <summary>
        /// Delete a secret from Key Vault
        /// </summary>
        /// <param name="keyvaultName">ID of the secret</param>
        /// <returns>keyName that was deleted</returns>
        public async Task<string> DeleteSecretAsync(string keyvaultName, string keyName)
        {
            var secretUrl = GetKeyVaultSecretUrl(keyvaultName);
            var secret = await _keyVaultClient.DeleteSecretAsync(secretUrl, keyName);
            return keyName;
        }

        /// <summary>
        /// Resolve the secret if it is in the secret uri format 'keyvault:///{keyvaultName}/{secret name}',
        /// or else return the origin string
        /// </summary>
        /// <param name="secretUri"></param>
        /// <returns></returns>
        public async Task<string> GetSecretFromKeyvaultAsync(string secretUri)
        {
            if(SecretUriParser.TryParseSecretUri(secretUri, out string keyvaultName, out string secretName))
            {
                var secretValue = await GetSecretStringAsync(keyvaultName, secretName);
                return secretValue;
            }
            else
            {
                return secretUri;
            }
        }

        /// <summary>
        /// Get the url for the secret name
        /// </summary>
        /// <param name="secretName">Name of the secret being retrieved</param>
        /// <returns></returns>
        //private string GetKeyVaultSecretUrl(string secretName)
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
