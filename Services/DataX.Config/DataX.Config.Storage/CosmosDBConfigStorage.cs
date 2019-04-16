// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json.Linq;
using DataX.Config.ConfigDataModel;
using DataX.Contract;
using DataX.Utility.CosmosDB;
using DataX.Utility.KeyVault;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace DataX.Config.Storage
{
    /// <summary>
    /// DesignTime storage implementation on cosmosDB
    /// </summary>
    [Export(typeof(IDesignTimeConfigStorage))]
    public class CosmosDBConfigStorage : IDesignTimeConfigStorage
    {
        public const string ConfigSettingName_DesignTimeCosmosDBConnectionString = "designTimeCosmosDbConnectionString";
        public const string ConfigSettingName_DesignTimeCosmosDBDatabaseName = "designTimeCosmosDbDatabaseName";

        private readonly CosmosDBUtility _db;

        [ImportingConstructor]
        public CosmosDBConfigStorage(ConfigGenConfiguration configGenConfiguration, IKeyVaultClient keyvaultClient)
        {
            string keyVaultName = configGenConfiguration[Constants.ConfigSettingName_ServiceKeyVaultName];

            string connectionString = keyvaultClient
                .ResolveSecretUriAsync(configGenConfiguration[ConfigSettingName_DesignTimeCosmosDBConnectionString])
                .Result;
            Ensure.NotNull(connectionString, "connectionString");

            string dbName = keyvaultClient
                .ResolveSecretUriAsync(configGenConfiguration[ConfigSettingName_DesignTimeCosmosDBDatabaseName])
                .Result;
            Ensure.NotNull(dbName, "dbName");

            _db = new CosmosDBUtility(connectionString, dbName);
        }

        public static string[] Collect(IEnumerable<string> result)
        {
            return result.ToArray();
        }

        /// <summary>
        /// Get all docs in the collection
        /// </summary>
        /// <param name="collectionName"></param>
        /// <returns></returns>

        public async Task<string[]> GetAll(string collectionName)
        {
            var result = await _db.FindAll(collectionName);
            return Collect(result);
        }

        /// <summary>
        /// Get document based on field value
        /// </summary>
        /// <param name="fieldValue"></param>
        /// <param name="fieldName"></param>
        /// <param name="collectionName"></param>
        /// <returns></returns>
        public async Task<string[]> GetByFieldValue(string fieldValue, string fieldName, string collectionName)
        {
            var result = await _db.FindByFieldValue(collectionName, fieldName, fieldValue);
            return Collect(result);
        }

        /// <summary>
        /// Get document by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="collectionName"></param>
        /// <returns></returns>

        public async Task<string> GetByName(string name, string collectionName)
        {
            var result = await _db.FindByName(collectionName, name);
            return result.SingleOrDefault();
        }

        /// <summary>
        /// Get documents by set of names
        /// </summary>
        /// <param name="names"></param>
        /// <param name="collectionName"></param>
        /// <returns></returns>
        public async Task<string[]> GetByNames(string[] names, string collectionName)
        {
            var result = await _db.FindByNames(collectionName, names);
            return Collect(result);
        }

        /// <summary>
        /// Save the document 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="content"></param>
        /// <param name="collectionName"></param>
        /// <returns></returns>
        public async Task<Result> SaveByName(string name, string content, string collectionName)
        {
            var result = await _db.UpsertDocumentByName(collectionName, name, content);
            return new SuccessResult(result);
        }

        /// <summary>
        /// Update a portion of the document
        /// </summary>
        /// <param name="partialFieldValue"></param>
        /// <param name="fieldPath"></param>
        /// <param name="name"></param>
        /// <param name="collectionName"></param>
        /// <returns></returns>
        public async Task<Result> UpdatePartialByName(string partialFieldValue, string fieldPath, string name, string collectionName)
        {
            var result = await _db.UpsertPartialDocumentByName(collectionName, name, fieldPath, partialFieldValue);
            return new SuccessResult(result);
        }

        public async Task<Result> DeleteByName(string name, string collectionName)
        {
            var result = await _db.DeleteDocument(collectionName, "name", name);
            return new SuccessResult(result);
        }
    }
}
