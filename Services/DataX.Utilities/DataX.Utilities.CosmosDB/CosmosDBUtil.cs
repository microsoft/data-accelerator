// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using DataX.Contract;
using System;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace DataX.Utilities.CosmosDB
{
    public class CosmosDBUtil
    {
        private readonly string _endpoint;
        private readonly string _userName;
        private readonly string _password;

        private MongoClient _mongoClient;

        private MongoClient DocumentClient
        {
            get
            {
                if (_mongoClient == null)
                {
                    _mongoClient = CreateClient();
                }

                return _mongoClient;
            }
        }

        public CosmosDBUtil(string endPoint, string userName, string password)
        {
            _endpoint = endPoint;
            _userName = userName;
            _password = password;
        }

        private MongoClient CreateClient()
        {
            try
            {
                MongoClientSettings settings = new MongoClientSettings
                {
                    Server = new MongoServerAddress(_endpoint, 10250),
                    UseSsl = true,
                    SslSettings = new SslSettings
                    {
                        EnabledSslProtocols = SslProtocols.Tls12
                    }
                };

                MongoIdentity identity = new MongoInternalIdentity("production", _userName);
                MongoIdentityEvidence evidence = new PasswordEvidence(_password);

                settings.Credential = new MongoCredential("SCRAM-SHA-1", identity, evidence);

                MongoClient client = new MongoClient(settings);
                return client;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<ApiResult> UploadConfigToDocumentDB(string db, string collection, JObject item)
        {

            if (string.IsNullOrEmpty(db) || string.IsNullOrEmpty(collection) || item == null)
            {
                return ApiResult.CreateError("CosmosDBUtil: db, collection or item cannot be null");
            }
            try
            {
                var database = DocumentClient.GetDatabase(db);
                var col = database.GetCollection<BsonDocument>(collection);



                var document = BsonSerializer.Deserialize<BsonDocument>(item.ToString());
                var name = item.Value<string>("name");
                var filter = Builders<BsonDocument>.Filter.Eq("name", name);

                var response = await col.ReplaceOneAsync(filter, document, new UpdateOptions { IsUpsert = true });

                return ApiResult.CreateSuccess(response.UpsertedId != null? response.UpsertedId.ToString() : "");

            }
            catch (Exception ex)
            {
                return ApiResult.CreateError("CosmosDBUtil: " + ex.Message);
            }

        }

        public async Task<ApiResult> DownloadConfigFromDocumentDB(string db, string collection)
        {
            if (string.IsNullOrEmpty(db) || string.IsNullOrEmpty(collection))
            {
                return ApiResult.CreateError("CosmosDBUtil: db and collection names cannot be null");
            }
            try
            {
                var database = DocumentClient.GetDatabase(db);
                var col = database.GetCollection<BsonDocument>(collection);

                var cursor = await col.Find(_ => true).Project(Builders<BsonDocument>
                    .Projection.Exclude("_id"))
                    .ToListAsync();

                if (cursor != null && cursor.Count() > 0)
                {
                    var doc = cursor.FirstOrDefault();
                    if (doc != null)
                    {
                        return ApiResult.CreateSuccess(JObject.Parse(doc.ToJson()));
                    }
                }

                return ApiResult.CreateError("CosmosDBUtil: No item with the subscriptionId found");

            }
            catch (Exception ex)
            {
                return ApiResult.CreateError("CosmosDBUtil: " + ex.Message);
            }
        }

        /// <summary>
        /// DeleteConfigFromDocumentDB
        /// </summary>
        /// <param name="db">cosmosDB database</param>
        /// <param name="collection">cosmosDB collection</param>
        /// <param name="flowId">flowId</param>
        /// <returns>ApiResult which contains error or successful result as the case maybe</returns>
        public async Task<ApiResult> DeleteConfigFromDocumentDB(string db, string collection, string flowId)
        {
            if (string.IsNullOrEmpty(db) || string.IsNullOrEmpty(collection))
            {
                return ApiResult.CreateError("CosmosDBUtil: db and collection names cannot be null");
            }
            try
            {
                var database = DocumentClient.GetDatabase(db);
                var col = database.GetCollection<BsonDocument>(collection);
                var filter = Builders<BsonDocument>.Filter.Eq("name", flowId);
                var response = await col.DeleteOneAsync(filter);

                return ApiResult.CreateSuccess($"CosmosDBUtil: Deleted the cosmosDB entry for the flow: {flowId}");
            }
            catch (Exception ex)
            {
                return ApiResult.CreateError("CosmosDBUtil: " + ex.Message);
            }
        }
    }
}
