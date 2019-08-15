// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DataX.Utility.KeyVault
{
    public static class SecretUriParser
    {
        private static Regex _Reg = new Regex(@"^((keyvault|secretscope:?):\/\/)?([^:\/\s]+)(\/)(.*)?", RegexOptions.IgnoreCase);

        public static void ParseSecretUri(string secretUri, out string keyvaultName, out string secretName)
        {
            if (!TryParseSecretUri(secretUri, out keyvaultName, out secretName))
            {
                throw new ArgumentException($"cannot parse {secretUri}");
            }
        }

        public static bool TryParseSecretUri(string secretUri, out string keyvaultName, out string secretName)
        {
            if (secretUri == null)
            {
                keyvaultName = null;
                secretName = null;
                return false;
            }

            MatchCollection m = _Reg.Matches(secretUri);
            if (m == null || m.Count == 0)
            {
                keyvaultName = null;
                secretName = null;
                return false;
            }
            else
            {
                keyvaultName = m[0].Groups[3].Value;
                secretName = m[0].Groups[5].Value;
                return true;
            }
        }

        public static string ComposeUri(string keyvaultName, string secretName, string uriPrefix)
        {
            return $"{uriPrefix}://{keyvaultName}/{secretName}";
        }
    }
}
