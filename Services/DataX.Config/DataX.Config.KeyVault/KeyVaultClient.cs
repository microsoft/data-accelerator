// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Contract;
using DataX.Utility.KeyVault;
using System;
using System.Composition;
using System.Threading.Tasks;

namespace DataX.Config.KeyVault
{
    [Export(typeof(IKeyVaultClient))]
    public class KeyVaultClient: IKeyVaultClient
    {
        public KeyVaultClient()
        {
        }

        public KeyVaultUtility GetKeyVault()
        {
            return new KeyVaultUtility();
        }

        public Task<string> GetSecretFromKeyVaultAsync(string keyvaultName, string secretName)
        {
            Ensure.NotNull(keyvaultName, "keyvaultName");
            Ensure.NotNull(secretName, "secretName");
            return GetKeyVault().GetSecretStringAsync(keyvaultName: keyvaultName, secretName: secretName);
        }


        public Task<string> ResolveSecretUriAsync(string secretUri)
        {
            return GetKeyVault().GetSecretFromKeyvaultAsync(secretUri);
        }

        public async Task<string> SaveSecretAsync(string keyvaultName, string secretUri, string secret, string uriPrefix)
        {
            await GetKeyVault().SaveSecretStringAsync(keyvaultName, secretUri, secret);
            return SecretUriParser.ComposeUri(keyvaultName, secretUri, uriPrefix);
        }

        public async Task<string> SaveSecretAsync(string keyvaultName, string secretName, string secretValue, string uriPrefix, bool hashSuffix = false)
        {
            var finalSecretName = hashSuffix ? (secretName + "-" + HashGenerator.GetHashCode(secretValue)) : secretName;
            await GetKeyVault().SaveSecretStringAsync(keyvaultName, finalSecretName, secretValue);
            return SecretUriParser.ComposeUri(keyvaultName, finalSecretName, uriPrefix);
        }
    }
}
