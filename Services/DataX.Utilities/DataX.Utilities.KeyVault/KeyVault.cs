// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Text.RegularExpressions;

namespace DataX.Utilities.KeyVault
{
    public static class KeyVault
    {
        public static string GetSecretFromKeyvault(string secretUri)
        {
            ParseSecretUri(secretUri, out string keyvaultName, out string secretName);

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
            ParseSecretUri(secretUri, out string keyvaultName, out string secretName);

            var secretValue = KeyVaultUtility.SaveSecretToKeyvault(keyvaultName, secretName, value).Result;
            return secretValue;
        }

        private static void ParseSecretUri(string secretUri, out string keyvaultName, out string secretName)
        {
            try
            {
                Regex reg = new Regex(@"^((keyvault|secretscope:?):\/\/)?([^:\/\s]+)(\/)(.*)?", RegexOptions.IgnoreCase);
                MatchCollection m = reg.Matches(secretUri);
                keyvaultName = m[0].Groups[3].Value;
                secretName = m[0].Groups[5].Value;
            }
            catch (Exception)
            {
                throw new Exception("Can't parse keyvault from string:" + secretUri);
            }
        }
    }
}
