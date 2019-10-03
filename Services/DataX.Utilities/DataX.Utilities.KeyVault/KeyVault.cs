// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Utility.KeyVault;
using System;
using System.Text.RegularExpressions;

namespace DataX.Utilities.KeyVault
{
    public static class KeyVault
    {
        public static string GetSecretFromKeyvault(string secretUri)
        {
            SecretUriParser.ParseSecretUri(secretUri, out string keyvaultName, out string secretName);

            var secretValue = GetSecretFromKeyvault(keyvaultName, secretName);
            return secretValue;

        }
        public static string GetSecretFromKeyvault(string keyvaultName, string secretName)
        {
            var secretValue = KeyVaultUtility.GetSecretFromKeyvault(keyvaultName, secretName).Result;
            return secretValue;
        }

        public static string GetSecretsAndDeleteFromKeyvault(string keyvaultName, string flowId)
        {
            return KeyVaultUtility.GetSecretsAndDeleteSecretsFromKeyVault(keyvaultName, flowId).Result;
        }

        public static string SaveSecretToKeyvault(string secretUri, string value)
        {
            SecretUriParser.ParseSecretUri(secretUri, out string keyvaultName, out string secretName);

            var secretValue = KeyVaultUtility.SaveSecretToKeyvault(keyvaultName, secretName, value).Result;
            return secretValue;
        }
    }
}
