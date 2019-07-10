// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using DataX.Contract;
using DataX.Utility.CosmosDB;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace DataX.Config.ConfigurationProviders
{
    /// <summary>
    /// ConfigProvider based on Cosmos DB.
    /// </summary>
    [Shared]
    [Export(typeof(IConfigGenConfigurationProvider))]
    public class CosmosDbConfigurationProvider : IConfigGenConfigurationProvider
    {
        public const string ConfigSettingName_CosmosDBConfig_ConnectionString = "cosmosDBConfigConnectionString";
        public const string ConfigSettingName_CosmosDBConfig_DatabaseName = "cosmosDBConfigDatabaseName";
        public const string ConfigSettingName_CosmosDBConfig_CollectionName= "cosmosDBConfigCollectionName";

        private IDictionary<string, string> _settings { get; set; }
        
        [ImportingConstructor]
        public CosmosDbConfigurationProvider(IKeyVaultClient kv)
        {
            async Task<string> resolveKey(string configSettingName)
            {
                var key = InitialConfiguration.Get(configSettingName);
                Ensure.NotNull(key, configSettingName);
                var value = await kv.ResolveSecretUriAsync(key);
                Ensure.NotNull(value, "resolved_" + configSettingName);
                return value;
            }

            var connectionString = resolveKey(ConfigSettingName_CosmosDBConfig_ConnectionString).Result;
            var dbName = resolveKey(ConfigSettingName_CosmosDBConfig_DatabaseName).Result;
            var collectionName = resolveKey(ConfigSettingName_CosmosDBConfig_CollectionName).Result;

            var cosmosDBUtil = new CosmosDBUtility(connectionString, dbName);
            var settings = cosmosDBUtil.FindOne(collectionName).Result;
            var settingsDictionary = DeserializeToDictionary(settings);
            _settings = new Dictionary<string, string>(settingsDictionary.Select(x => KeyValuePair.Create<string, string>(x.Key, x.Value.ToString())));
        }

        public bool TryGet(string key, out string value)
        {
            return _settings.TryGetValue(key, out value);
        }

        /// <summary>
        /// Convert JSON to dictionary
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        private static Dictionary<string, object> DeserializeToDictionary(string json)
        {
            
            var topLevelValues = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            var expandedValues = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> d in topLevelValues)
            {
                if (d.Value != null && d.Value is Newtonsoft.Json.Linq.JObject)
                {
                    expandedValues.Add(d.Key, DeserializeToDictionary(d.Value.ToString()));
                }
                else
                {
                    expandedValues.Add(d.Key, d.Value);
                }
            }
            return expandedValues;
        }

        public int GetOrder()
        {
            return 1000;
        }

        public IDictionary<string, string> GetAllSettings()
        {
            return _settings;
        }
    }
}
