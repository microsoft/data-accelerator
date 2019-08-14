// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Config.KeyVault;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataX.Config.Test.Mock
{
    [Shared]
    [Export(typeof(IKeyVaultClient))]
    public class KeyVaultClient : IKeyVaultClient
    {
        public async Task<string> GetSecretFromKeyVaultAsync(string keyvaultName, string secretName)
        {
            if (secretName == "keyvault://somekeyvault/configgentest-output-5D09E8DD98332F8B2723EC5C1BCE9AD1")
            {
                return await Task.FromResult(@"DefaultEndpointsProtocol=https;AccountName=testaccount;AccountKey=testkey;EndpointSuffix=core.windows.net");
            }
            throw new Exception("secret not found");
        }
      

        public async Task<string> ResolveSecretUriAsync(string secretUri)
        {
            if (secretUri == "keyvault://somekeyvault/configgenbatchtest-input-0-inputConnection")
            {
                return await Task.FromResult(@"DefaultEndpointsProtocol=https;AccountName=testaccount;AccountKey=testkey;EndpointSuffix=core.windows.net");
            }
            return secretUri;
        }

        public async Task<string> SaveSecretAsync(string keyvaultName, string secretName, string secretValue, string sparkType, bool hashSuffix = false)
        {
            var uriPrefix = GetUriPrefix(sparkType);
            var finalSecretName = hashSuffix ? (secretName + "-" + HashGenerator.GetHashCode(secretValue)) : secretName;
            await Task.Yield();
            return $"{uriPrefix}://{keyvaultName}/{finalSecretName}";
        }

        public async Task<string> SaveSecretAsync(string secretUri, string secretValue)
        {
            await Task.Yield();
            return secretUri;
        }

        public string GetUriPrefix(string sparkType)
        {
            return (sparkType != null && sparkType == Constants.SparkTypeDataBricks) ? Constants.PrefixSecretScope : Constants.PrefixKeyVault;
        }
    }
}
