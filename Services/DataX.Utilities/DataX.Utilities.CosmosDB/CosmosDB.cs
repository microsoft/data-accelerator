// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json.Linq;
using DataX.Contract;
using System.Threading.Tasks;

namespace DataX.Utilities.CosmosDB
{
    public static class CosmosDB
    {
        public static async Task<ApiResult> UploadConfigToDocumentDB(string db, string endPoint, string user, string password, string collection, JObject item)
        {
            CosmosDBUtil cosmosDBUtility = new CosmosDBUtil(endPoint, user, password);
            return await cosmosDBUtility.UploadConfigToDocumentDB(db, collection, item);
        }

        public static async Task<ApiResult> DownloadConfigFromDocumentDB(string db, string endPoint, string user, string password, string collection)
        {
            CosmosDBUtil cosmosDBUtility = new CosmosDBUtil(endPoint, user, password);
            return await cosmosDBUtility.DownloadConfigFromDocumentDB(db, collection);
        }

        public static async Task<ApiResult> DeleteConfigFromDocumentDB(string db, string endPoint, string user, string password, string collection, string flowId)
        {
            CosmosDBUtil cosmosDBUtility = new CosmosDBUtil(endPoint, user, password);
            return await cosmosDBUtility.DeleteConfigFromDocumentDB(db, collection, flowId);
        }
    }
}
