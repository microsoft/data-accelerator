// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using MongoDB.Driver.Linq;
using System.Collections.Generic;

namespace DataX.Utility.CosmosDB
{
    /// <summary>
    /// Utility class for interacting with CosmosDB
    /// </summary>
    public class CosmosDBUtility
    {
        private readonly string _endpoint;
        private readonly string _userName;
        private readonly string _password;
        private readonly string _databaseName;
        private readonly int _port;

        private MongoClient _mongoClient;
        private IMongoDatabase _database;

        private MongoClient documentClient
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

        private IMongoDatabase database
        {
            get
            {
                if (_database == null)
                {
                    _database = documentClient.GetDatabase(_databaseName);
                }

                return _database;
            }
        }

        public CosmosDBUtility(string connectionString, string databaseName)
        {
            var namePassword = ParseCosmosDBUserNamePassword(connectionString);

            _endpoint = ParseCosmosDBEndPoint(connectionString);
            _port = ParseCosmosDBPortNumber(connectionString);
            _userName = namePassword.Split(new char[] { ':' })[0];
            _password = namePassword.Split(new char[] { ':' })[1];
            _databaseName = databaseName;
        }

        private MongoClient CreateClient()
        {
            try
            {
                MongoClientSettings settings = new MongoClientSettings()
                {
                    Server = new MongoServerAddress(_endpoint, _port),
                    UseSsl = true,
                    SslSettings = new SslSettings()
                };

                MongoIdentity identity = new MongoInternalIdentity(_databaseName, _userName);
                MongoIdentityEvidence evidence = new PasswordEvidence(_password);

                settings.Credential = new MongoCredential("SCRAM-SHA-1", identity, evidence);
                settings.SslSettings.EnabledSslProtocols = SslProtocols.Tls12;

                MongoClient client = new MongoClient(settings);
                return client;
            }
            catch (Exception)
            {
                throw;
            }
        }
        
        public static readonly Regex LeadingDollarSign = new Regex("\"\\$([^\"]*\"\\s*:)");
        public static readonly Regex DotInPropertyName = new Regex("\"[^\"\\.]*\\.[^\"]*\"\\s*:");
        public static readonly MatchEvaluator ReplaceDotEvaluator = new MatchEvaluator(m => m.Value.Replace('.', '\uff0e'));

        public static string Sanitize(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return json;
            }
            else
            {
                json = LeadingDollarSign.Replace(json, "\"_S_$1");
                json = DotInPropertyName.Replace(json, ReplaceDotEvaluator);
                return json;
            }
        }

        public static string Desanitize(string json)
        {
            return json?.Replace('\uff0e', '.')?.Replace("\"_S_", "\"$");
        }

        public async Task<string> UpsertDocumentByName(string collection, string name, string doc)
        {
            if (string.IsNullOrEmpty(collection) || string.IsNullOrEmpty(doc))
            {
                throw new ArgumentException("CosmosDBUtil: Collection or document cannot be empty");
            }

            var col = database.GetCollection<BsonDocument>(collection);
            var document = BsonSerializer.Deserialize<BsonDocument>(Sanitize(doc));
            var filter = Builders<BsonDocument>.Filter.Eq("name", name);
            var response = await col.ReplaceOneAsync(filter, document, new UpdateOptions { IsUpsert = true });
            
            if (!response.IsAcknowledged)
            {
                throw new Exception($"CosmosDB does not ackknowledge:'{response.ToJson()}'");
            }

            if (response.MatchedCount>1)
            {
                throw new Exception($"Expected 1 but matched {response.MatchedCount} docs for name '{name}' in collection '{collection}'");
            }

            if (response.IsModifiedCountAvailable && response.ModifiedCount > 1)
            {
                throw new Exception($"Expected 1 but updated {response.ModifiedCount} docs for name '{name}' in collection '{collection}'");
            }

            return response.UpsertedId != null ? response.UpsertedId.ToString() : "";
        }

        public async Task<string> UpsertPartialDocumentByName(string collection, string name, string fieldName, string fieldValue)
        {
            if (string.IsNullOrEmpty(collection) || string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("CosmosDBUtil: Collection or item cannot be null");
            }

            var col = database.GetCollection<BsonDocument>(collection);
            var filter = Builders<BsonDocument>.Filter.Eq("name", name);

            var document = fieldValue == null ? null : BsonSerializer.Deserialize<BsonValue>(Sanitize(fieldValue));
            var update = (fieldName == "gui") ? Builders<BsonDocument>.Update.Set(fieldName, document).Set("displayName", document["displayName"]) : Builders<BsonDocument>.Update.Set(fieldName, document);

            var response = await col.UpdateOneAsync(filter, update);

            if (!response.IsAcknowledged)
            {
                throw new Exception($"CosmosDB does not ackknowledge:'{response.ToJson()}'");
            }

            if (response.MatchedCount > 1)
            {
                throw new Exception($"Expected 1 but matched {response.MatchedCount} docs for name '{name}' in collection '{collection}'");
            }

            if (response.IsModifiedCountAvailable && response.ModifiedCount > 1)
            {
                throw new Exception($"Expected 1 but updated {response.ModifiedCount} docs for name '{name}' in collection '{collection}'");
            }

            return response.UpsertedId != null ? response.UpsertedId.ToString() : "";
        }



        /// <summary>
        /// DeleteConfigFromDocumentDB
        /// </summary>
        /// <param name="db">cosmosDB database</param>
        /// <param name="collection">cosmosDB collection</param>
        /// <param name="flowId">flowId</param>
        /// <returns>ApiResult which contains error or successful result as the case maybe</returns>
        public async Task<string> DeleteDocument(string collection, string fieldName, string fieldValue)
        {
            if (string.IsNullOrEmpty(collection))
            {
                throw new ArgumentException("CosmosDBUtil: Collection name cannot be null");
            }

            var col = database.GetCollection<BsonDocument>(collection);
            var filter = Builders<BsonDocument>.Filter.Eq(fieldName, fieldValue);
            var response = await col.DeleteOneAsync(filter);

            return response.ToJson();
        }

        public async Task<string> FindOne(string collection)
        {
            if (string.IsNullOrEmpty(collection))
            {
                throw new ArgumentException("CosmosDBUtil: collection name cannot be null");
            }

            var filter = Builders<BsonDocument>.Filter.Empty;
            var result = await Query(collection, filter);
            return result.FirstOrDefault();
        }

        public async Task<IEnumerable<string>> FindAll(string collection)
        {
            if (string.IsNullOrEmpty(collection))
            {
                throw new ArgumentException("CosmosDBUtil: collection name cannot be null");
            }

            var filter = Builders<BsonDocument>.Filter.Empty;
            return await Query(collection, filter);
        }

        public async Task<IEnumerable<string>> FindByFieldValue(string collection, string fieldName, string fieldValue)
        {
            if (string.IsNullOrEmpty(collection) || string.IsNullOrEmpty(fieldName) || string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentException("CosmosDBUtil: collection name, fieldName, fieldValue cannot be null");
            }

            var filter = Builders<BsonDocument>.Filter.Eq(fieldName, fieldValue);
            return await Query(collection, filter);
        }

        public async Task<IEnumerable<string>> FindByFieldValues(string collection, string fieldName, string[] fieldValues)
        {
            if (string.IsNullOrEmpty(collection) || string.IsNullOrEmpty(fieldName) || fieldValues == null)
            {
                throw new ArgumentException("CosmosDBUtil: collection name, fieldNames, fieldValues cannot be null");
            }

            var filter = Builders<BsonDocument>.Filter.In(fieldName, fieldValues);
            return await Query(collection, filter);
        }

        public async Task<IEnumerable<string>> FindByName(string collection, string name)
        {
            if (string.IsNullOrEmpty(collection) || string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("CosmosDBUtil: collection name or name cannot be null");
            }

            return await FindByFieldValue(collection, "name", name);
        }

        public async Task<IEnumerable<string>> FindByNames(string collection, string[] names)
        {
            if (string.IsNullOrEmpty(collection) || names == null)
            {
                throw new ArgumentException("CosmosDBUtil: collection name or names cannot be null");
            }

            return await FindByFieldValues(collection, "name", names);
        }


        private async Task<IEnumerable<string>> Query(string collection, FilterDefinition<BsonDocument> filter)
        {
            var col = database.GetCollection<BsonDocument>(collection);

            // Remove auto generated _id field from the result
            var bsonDocs = await col.Find(filter).Project(Builders<BsonDocument>
             .Projection.Exclude("_id"))
             .ToListAsync();

            IEnumerable<string> result = new List<string>();

            if (bsonDocs != null && bsonDocs.Count() > 0)
            {
                result = bsonDocs.Select(x => Desanitize(x.ToJson()));
            }

            return result;
        }

        private string ParseCosmosDBEndPoint(string connectionString)
        {
            string matched = Regex.Match(connectionString, @"(?<===@)(.*)(?=:[0-9])").Value;
            return matched;
        }

        private string ParseCosmosDBUserNamePassword(string connectionString)
        {
            string matched = Regex.Match(connectionString, @"(?<=//)(.*)(?=@)").Value;
            return matched;
        }


        private int ParseCosmosDBPortNumber(string connectionString)
        {
            var matched = Regex.Match(connectionString, @"(?<===@)(.*):(?<port>[0-9]+)");
            var port = matched.Groups["port"].Value;
            return int.Parse(port);
        }
    }
}
