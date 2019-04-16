// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using LiteDB;
using DataX.Config.ConfigDataModel;
using DataX.Contract;
using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DataX.Config.Local
{

    /// <summary>
    /// Design time storage based on liteDB
    /// </summary>
    [Export(typeof(IDesignTimeConfigStorage))]
    public class LocalDesignTimeStorage : IDesignTimeConfigStorage
    {
        private readonly string _localDbName = "local.db";

        // Property which stores the full path to local db file
        public static string _LocalDb { get; private set; }

        public LocalDesignTimeStorage()
        {
            _LocalDb = Path.Combine(System.Environment.CurrentDirectory, _localDbName);
        }

        public static string[] Collect(IEnumerable<BsonDocument> result)
        {
            return result.Select(s =>
            {
                var r = s.Remove("_id");
                return Desanitize(s.ToString());
            }).ToArray();
        }

        public async Task<string[]> GetAll(string collectionName)
        {
            await Task.Yield();
            using (var db = new LiteDatabase(_LocalDb))
            {
                var col = db.GetCollection(collectionName);
                var docs = col.FindAll();
                return Collect(docs);
            }
        }

        public async Task<string[]> GetByFieldValue(string matchValue, string field, string collectionName)
        {
            await Task.Yield();
            using (var db = new LiteDatabase(_LocalDb))
            {
                var col = db.GetCollection(collectionName);
                var docs = col.Find(Query.EQ(field, matchValue));
                return Collect(docs);
            }
        }

        public async Task<string> GetByName(string name, string collectionName)
        {
            await Task.Yield();
            using (var db = new LiteDatabase(_LocalDb))
            {
                var col = db.GetCollection(collectionName);
                var doc = col.FindOne(Query.EQ("name", name));
                doc?.Remove("_id");
                return Desanitize(doc?.ToString());
            }
        }

        public async Task<string[]> GetByNames(string[] names, string collectionName)
        {
            await Task.Yield();
            using (var db = new LiteDatabase(_LocalDb))
            {
                var col = db.GetCollection(collectionName);
                var namesArray = names.Select(x => new BsonValue(x)).ToArray();
                IEnumerable<BsonDocument> docs = col.Find(Query.In("name", namesArray));
                return Collect(docs);
            }
        }

        public async Task<Result> SaveByName(string name, string content, string collectionName)
        {
            if (string.IsNullOrEmpty(collectionName) || content == null)
            {
                throw new ArgumentException("DesignTimeStorage: Collection or content cannot be null");
            }

            using (var db = new LiteDatabase(_LocalDb))
            {
                var col = db.GetCollection(collectionName);
                BsonValue bVal = JsonSerializer.Deserialize(Sanitize(content));

                var doc = col.FindOne(Query.EQ("name", name));
                bool result;

                // If new doc, insert it
                if (doc == null)
                {
                    result = col.Upsert(bVal.AsDocument);
                }
                else
                {
                    // If existing doc, add the _id field to updated doc so that it can update existing doc.
                    // Without _id, update will fail
                    BsonDocument updatedDoc = bVal.AsDocument;
                    var x = doc["_id"];
                    updatedDoc.Add("_id", doc["_id"]);

                    result = col.Update(updatedDoc);
                }
                return result ? await GetSuccessResult() : await GetFailedResult();
            }
        }

        public async Task<Result> UpdatePartialByName(string partialFieldValue, string fieldPath, string name, string collectionName)
        {
            if (string.IsNullOrEmpty(collectionName) || string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("DesignTimeStorage: Collection or item cannot be null");
            }

            using (var db = new LiteDatabase(_LocalDb))
            {
                var col = db.GetCollection(collectionName);
                var doc = col.FindOne(Query.EQ("name", name));

                // If the passed in field value is not JSON, convert it to JSON
                var jsonField = IsJson(partialFieldValue) ? partialFieldValue : Newtonsoft.Json.JsonConvert.SerializeObject(partialFieldValue);
                BsonValue bVal = partialFieldValue == null ? null : JsonSerializer.Deserialize(Sanitize(jsonField));
                doc.SetField(fieldPath, bVal);
                var result = col.Update(doc);
                return result ? await GetSuccessResult() : await GetFailedResult($"UpdatePartialByName to {fieldPath} failed for {name}");
            }
        }

        public async Task<Result> DeleteByName(string name, string collectionName)
        {
            if (string.IsNullOrEmpty(collectionName) || string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("DesignTimeStorage: Collection or item cannot be null");
            }

            using (var db = new LiteDatabase(_LocalDb))
            {
                var col = db.GetCollection(collectionName);
                int deletionCount = col.Delete(Query.EQ("name", name));
                return deletionCount > 0 ? await GetSuccessResult() : await GetFailedResult($"Deletion of {name} in {collectionName} failed");
            }
        }

        public void DropCollection(string collectionName)
        {
            using (var db = new LiteDatabase(_LocalDb))
            {
                db.DropCollection(collectionName);
            }
        }

        public static string Sanitize(string json)
        {
            return json?.Replace("\"$", "\"_S_");
        }

        public static string Desanitize(string json)
        {
            return json?.Replace("\"_S_", "\"$");
        }

        private async Task<Result> GetSuccessResult()
        {
            await Task.Yield();
            return new SuccessResult("Success");
        }

        private async Task<Result> GetFailedResult(string msg = "")
        {
            await Task.Yield();
            return new FailedResult(msg);
        }

        public static bool IsJson(string jsonData)
        {
            return jsonData.Trim().Substring(0, 1).IndexOfAny(new[] { '[', '{' }) == 0;
        }
    }
}
