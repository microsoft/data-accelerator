// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System.Composition;
using System.Threading.Tasks;

namespace DataX.Config.Local
{
    /// <summary>
    /// Local Keyvault client
    /// </summary>
    [Shared]
    [Export(typeof(IKeyVaultClient))]
    public class KeyVaultClient : IKeyVaultClient
    {
        public Task<string> GetSecretFromKeyVaultAsync(string keyvaultName, string secretName)
        {
            return Task.FromResult(secretName);
        }


        public Task<string> ResolveSecretUriAsync(string secretUri)
        {           
            return Task.FromResult(secretUri);
        }    

        public Task<string> SaveSecretAsync(string keyvaultName, string secretName, string secretValue, string uriPrefix, bool hashSuffix = false)
        {
            return Task.FromResult(secretValue);
        }
    }
}
