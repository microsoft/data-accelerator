// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Contract.Exception;
using DataX.Utilities.KeyVault;
using DataX.Utility.KeyVault;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace DataX.Flow.Common
{
    public static class Helper
    {
        private static readonly Regex _ConnectionStringFormat = new Regex("^endpoint=([^;]*);username=([^;]*);password=(.*)$");

        /// <summary>
        /// Replaces tokens in the templates
        /// </summary>
        /// <param name="template"></param>
        /// <param name="values"></param>
        /// <returns>Translated template</returns>
        public static string TranslateOutputTemplate(string template, Dictionary<string, string> values)
        {
            foreach (var kvp in values)
            {
                template = template.Replace($"<@{kvp.Key}>", kvp.Value);
            }

            return template;
        }

        /// <summary>
        /// Checks if the value is a keyvault and if it is, gets the value from a keyvault
        /// Otherwise, returns as is
        /// </summary>
        /// <param name="value"></param>
        /// <returns>value or value from secret</returns>
        public static string GetSecretFromKeyvaultIfNeeded(string value)
        {
            if (IsKeyVault(value))
            {
                return KeyVault.GetSecretFromKeyvault(value);
            }

            return value;
        }

        /// <summary>
        /// Checks if it is a secret
        /// </summary>
        /// <param name="value"></param>
        /// <returns>true if it is a secret, otherwise false</returns>
        public static bool IsKeyVault(string value)
        {
            return value.StartsWith(GetKeyValutNamePrefix()) || value.StartsWith(GetSecretScopePrefix());
        }

        /// <summary>
        /// Composes a keyVault uri
        /// </summary>
        /// <param name="keyvaultName"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="postfix"></param>
        /// <returns>keyVault uri</returns>
        public static string GetKeyVaultName(string keyvaultName, string key, string sparkType, string value = "", bool postfix = true)
        {
            if (postfix)
            {
                key = key + $"-{Helper.GetHashCode(value)}";
            }

            return (sparkType == Config.ConfigDataModel.Constants.SparkTypeDataBricks) ? $"{GetSecretScopePrefix()}{keyvaultName}/{key}" : $"{GetKeyValutNamePrefix()}{keyvaultName}/{key}";
        }

        /// <summary>
        /// Generates a new secret and adds to a list. And the items in the list will be stored in a keyvault 
        /// </summary>
        /// <param name="keySecretList"></param>
        /// <param name="keyvaultName"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="postfix"></param>
        /// <returns>secret name</returns>
        public static string GenerateNewSecret(Dictionary<string, string> keySecretList, string keyvaultName, string key, string sparkType, string value, bool postfix = true)
        {
            key = GetKeyVaultName(keyvaultName, key, sparkType, value, postfix);

            keySecretList.TryAdd(key, value);

            return key;
        }

        /// <summary>
        /// Get the prefix for keyvault uri
        /// </summary>
        /// <returns>the prefix for keyvault uri</returns>
        public static string GetKeyValutNamePrefix()
        {
            return "keyvault://";
        }

        /// <summary>
        /// Get the prefix for secretscope uri
        /// </summary>
        /// <returns>the prefix for keyvault uri</returns>
        public static string GetSecretScopePrefix()
        {
            return "secretscope://";
        }

        /// <summary>
        /// Parses the eventhub namespace from connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>eventhub namespace</returns>
        public static string ParseEventHubNamespace(string connectionString)
        {
            var key = "Endpoint";
            var map = ConnectionStringToKeyValueMap(connectionString);
            if (map.ContainsKey(key))
            {
                var value = map[key];
                var match = Regex.Match(value, @"sb:\/\/(.*?)\.servicebus\.windows\.net");
                if (match.Success && match.Groups.Count == 2)
                {
                    return match.Groups[1].Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Get BootstrapServers from connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>bootstrapservers</returns>
        public static string TryGetBootstrapServers(string connectionString)
        {
            var bootstrapServers = connectionString;

            if (connectionString.StartsWith("Endpoint=sb", StringComparison.OrdinalIgnoreCase))
            {
                Regex reg = new Regex(@"sb?:\/\/([\w\d\.]+).*", RegexOptions.IgnoreCase);
                var m = reg.Match(connectionString);

                if (m != null && m.Success)
                {
                    bootstrapServers = m.Groups[1].Value + ":9093";
                }
            }

            return bootstrapServers;
        }

        /// <summary>
        /// Parses and generate a string array from a raw eventhub name string
        /// </summary>
        /// <param name="eventHubNames"></param>
        /// <returns>string array of eventhub names</returns>
        public static List<string> ParseEventHubNames(string eventHubNames)
        {
            if (string.IsNullOrWhiteSpace(eventHubNames))
            {
                return new List<string>();
            }

            var names = eventHubNames.Split(',').Select(s => s.Trim()).ToList();
            return names;
        }

        /// <summary>
        /// Parses the eventhub from connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>eventhub</returns>
        public static string ParseEventHub(string connectionString)
        {
            var key = "EntityPath";
            var map = ConnectionStringToKeyValueMap(connectionString);
            return map.ContainsKey(key) ? map[key] : null;
        }

        /// <summary>
        /// Parses the eventhub accesskey from connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>eventhub accesskey</returns>
        public static string ParseEventHubAccessKey(string connectionString)
        {
            var key = "SharedAccessKey";
            var map = ConnectionStringToKeyValueMap(connectionString);
            return map.ContainsKey(key) ? map[key] : null;
        }

        /// <summary>
        /// Parses the eventhub policy from connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>eventhub policy </returns>
        public static string ParseEventHubPolicyName(string connectionString)
        {
            var key = "SharedAccessKeyName";
            var map = ConnectionStringToKeyValueMap(connectionString);
            return map.ContainsKey(key) ? map[key] : null;
        }

        /// <summary>
        /// Parses the cosmosDB endpoint from connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>cosmosDB endpoint</returns>
        public static string ParseCosmosDBEndPoint(string connectionString)
        {
            string matched;
            try
            {
                matched = Regex.Match(connectionString, @"(?<===@)(.*)(?=:10255)").Value;
            }
            catch (Exception)
            {
                return "The connectionString does not have PolicyName";
            }

            return matched;
        }

        /// <summary>
        /// Parses the cosmosDB username from connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>cosmosDB username</returns>
        public static string ParseCosmosDBUserNamePassword(string connectionString)
        {
            string matched;
            try
            {
                matched = Regex.Match(connectionString, @"(?<=//)(.*)(?=@)").Value;
            }
            catch (Exception)
            {
                return "The connectionString does not have username/password";
            }

            return matched;
        }

        /// <summary>
        /// Generates a hashcode for the input
        /// </summary>
        /// <param name="value"></param>
        /// <returns>hashcode for the input</returns>
        public static string GetHashCode(string value)
        {
            HashAlgorithm hash = SHA256.Create();
            var hashedValue = hash.ComputeHash(Encoding.UTF8.GetBytes(value));
            return BitConverter.ToString(hashedValue).Replace("-", string.Empty).Substring(0, 32);
        }

        /// <summary>
        /// Generates a map for the input connectionstring
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>map for the connectionstring</returns>
        private static IDictionary<string, string> ConnectionStringToKeyValueMap(string connectionString)
        {
            var keyValueMap = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                var keyValues = connectionString.Split(';');

                foreach (var keyValue in keyValues)
                {
                    var keyValuePair = keyValue.Split(new char[] { '=' }, 2);
                    if (keyValuePair.Length == 2)
                    {
                        var key = keyValuePair[0];
                        var value = keyValuePair[1];
                        if (keyValueMap.ContainsKey(key))
                        {
                            keyValueMap[key] = value;
                        }
                        else
                        {
                            keyValueMap.Add(key, value);
                        }
                    }
                }
            }

            return keyValueMap;
        }

        /// <summary>
        /// PathResolver resolves the keyvault uri and gets the real path 
        /// </summary>
        /// <param name="path">path</param>
        /// <returns>Returns a string </returns>
        public static string PathResolver(string path)
        {
            if (path != null && Config.Utility.KeyVaultUri.IsSecretUri(path))
            {
                SecretUriParser.ParseSecretUri(path, out string keyvalut, out string secret);
                var secretUri = KeyVault.GetSecretFromKeyvault(keyvalut, secret);

                return secretUri;
            }
            return path;
        }

        /// <summary>
        /// ParseConnectionString the connection string to extract username and password
        /// </summary>
        /// <param name="connectionString">connectionString</param>
        /// <returns>SparkConnectionInfo object</returns>        
        public static SparkConnectionInfo ParseConnectionString(string connectionString)
        {
            if (connectionString == null)
            {
                throw new GeneralException($"connection string for livy client cannot be null");
            }

            var match = _ConnectionStringFormat.Match(connectionString);
            if (match == null || !match.Success)
            {
                throw new GeneralException($"cannot parse connection string to access livy service");
            }

            return new SparkConnectionInfo()
            {
                Endpoint = match.Groups[1].Value,
                UserName = match.Groups[2].Value,
                Password = match.Groups[3].Value
            };
        }

        /// <summary>
        /// This method converts wasbs path to dbfs file path
        /// </summary>
        /// <param name="filePath">wasbs file path</param>
        /// <param name="fileName">file name</param>
        /// <returns>Returns dbfs file path</returns>
        public static string ConvertToDbfsFilePath(string filePath, string fileName = "")
        {
            Regex opsPath = new Regex(@"wasbs:\/\/(.*)@(.*).blob.core.windows.net\/(.*)$", RegexOptions.IgnoreCase);
            var match = opsPath.Match(filePath);
            if (match.Success)
            {
                string result = Path.Combine(Config.ConfigDataModel.Constants.PrefixDbfs, Config.ConfigDataModel.Constants.PrefixDbfsMount + match.Groups[1].Value + "/", match.Groups[3].Value, fileName);
                return result;
            }
            else
            {
                throw new Exception("Cannot convert to DBFS file path");
            }
        }

        /// <summary>
        /// This method returns a string value based on the spark type
        /// </summary>
        /// <param name="sparkType">sparkType</param>
        /// <param name="valueForHDInsightEnv">Value to be used in case of HDInsight environment</param>
        /// <param name="valueForDatabricksEnv">Value to be used in case of Databricks environment</param>
        /// <returns>Returns string value based on spark type</returns>
        public static string SetValueBasedOnSparkType(string sparkType, string valueForHDInsightEnv, string valueForDatabricksEnv)
        {
            if (sparkType != Config.ConfigDataModel.Constants.SparkTypeDataBricks)
            {
                return valueForHDInsightEnv;
            }
            else
            {
                return valueForDatabricksEnv;
            }
        }
    }

    /// <summary>
    /// Class object for the connection info
    /// </summary>
    public class SparkConnectionInfo
    {
        public string Endpoint { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
