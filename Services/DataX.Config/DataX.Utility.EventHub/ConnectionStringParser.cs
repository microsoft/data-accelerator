// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DataX.Utility.EventHub
{
    public static class ConnectionStringParser
    {
        /// <summary>
        /// Parses the eventhub from connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>eventhub</returns>
        public static EventHubInfo ParseEventHub(string connectionString)
        {
            var map = ConnectionStringToKeyValueMap(connectionString);

            return new EventHubInfo(){
                Name = map.GetValueOrNull("EntityPath"),
                Namespace = ParseNamespaceFromEndpoint(map.GetValueOrNull("Endpoint")),
                Policy = map.GetValueOrNull("SharedAccessKeyName"),
                Key = map.GetValueOrNull("SharedAccessKey")
            };
        }

        public static string ParseNamespaceFromEndpoint(string endpoint)
        {
            if (endpoint == null)
            {
                return null;
            }

            var match = Regex.Match(endpoint, @"sb:\/\/(.*?)\.servicebus\.windows\.net");
            if (match.Success && match.Groups.Count == 2)
            {
                return match.Groups[1].Value;
            }
            else
            {
                return null;
            }
        }

        public static string GetValueOrNull(this IDictionary<string, string> dict, string key)
        {
            return dict.GetValueOrDefault(key, null);
        }

        public static string GetValueOrDefault(this IDictionary<string, string> dict, string key, string defaultValue)
        {
            if (dict.ContainsKey(key))
            {
                return dict[key];
            }
            else
            {
                return defaultValue;
            }
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
    }
}
