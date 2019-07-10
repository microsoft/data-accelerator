// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Contract;
using System.Composition;
using System.Threading.Tasks;
using DataX.Utilities.KeyVault;

namespace DataX.Config.KeyVault
{
    [Export(typeof(IKeyVaultClient))]
    public class KeyVaultClient: IKeyVaultClient
    {
        public KeyVaultClient()
        {
        }

        public KeyVaultManager GetKeyVault()
        {
            return new KeyVaultManager();
        }

        /// <summary>
        /// Get Secret
        /// </summary>
        /// <param name="keyvaultName"></param>
        /// <param name="secretName"></param>
        /// <returns></returns>
        public Task<string> GetSecretFromKeyVaultAsync(string keyvaultName, string secretName)
        {
            Ensure.NotNull(keyvaultName, "keyvaultName");
            Ensure.NotNull(secretName, "secretName");
            return GetKeyVault().GetSecretStringAsync(keyvaultName: keyvaultName, secretName: secretName);
        }


        /// <summary>
        /// Get secret from URI
        /// </summary>
        /// <param name="secretUri"></param>
        /// <returns></returns>
        public Task<string> ResolveSecretUriAsync(string secretUri)
        {
            return GetKeyVault().GetSecretStringAsync( secretUri );
        }

        /// <summary>
        /// Save secret
        /// </summary>
        /// <param name="keyvaultName">KV name</param>
        /// <param name="secretUri">Secret uri</param>
        /// <param name="secret">Secret</param>
        /// <returns></returns>
        public async Task<string> SaveSecretAsync(string keyvaultName, string secretUri, string secret, string sparkType)
        {
            var uriPrefix = (sparkType != null && sparkType == Constants.SparkTypeDataBricks) ? Constants.PrefixSecretScope : Constants.PrefixKeyVault;
            await GetKeyVault().SaveSecretStringAsync(keyvaultName, secretUri, secret);
            return SecretUriParser.ComposeUri(keyvaultName, secretUri, uriPrefix);
        }

        /// <summary>
        /// Save Secret async by kevault name, secretname.  
        /// </summary>
        /// <param name="keyvaultName">KV name</param>
        /// <param name="secretName">Secret name</param>
        /// <param name="secretValue">Secret</param>
        /// <param name="hashSuffix">Append this string to make secret unique</param>
        /// <returns></returns>
        public async Task<string> SaveSecretAsync(string keyvaultName, string secretName, string secretValue, string sparkType, bool hashSuffix = false)
        {
            var finalSecretName = hashSuffix ? (secretName + "-" + HashGenerator.GetHashCode(secretValue)) : secretName;
            var uriPrefix = (sparkType != null && sparkType == Constants.SparkTypeDataBricks) ? Constants.PrefixSecretScope : Constants.PrefixKeyVault;
            await GetKeyVault().SaveSecretStringAsync(keyvaultName, finalSecretName, secretValue);
            return SecretUriParser.ComposeUri(keyvaultName, finalSecretName, uriPrefix);
        }
    }
}
