// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using DataX.Config.ConfigDataModel;
using DataX.Contract;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace DataX.Config
{
    [Shared]
    [Export]
    public class SparkJobData
    {
        public const string DataCollectionName = "sparkJobs";

        [ImportingConstructor]
        public SparkJobData(IDesignTimeConfigStorage storage)
        {
            this.Storage = storage;
        }

        private IDesignTimeConfigStorage Storage { get; }

        public Task<Result> UpsertByName(string jobName, SparkJobConfig sparkJobConfig)
        {
            return Storage.SaveByName(jobName, sparkJobConfig.ToString(), DataCollectionName);
        }

        public async Task<SparkJobConfig[]> GetAll()
        {
            var jsons = await this.Storage.GetAll(DataCollectionName);
            return jsons.Select(x => SparkJobConfig.From(x)).ToArray();
        }

        public async Task<SparkJobConfig[]> GetByNames(string[] names)
        {
            var jsons = await this.Storage.GetByNames(names, DataCollectionName);
            return jsons.Select(x => SparkJobConfig.From(x)).ToArray();
        }

        public async Task<SparkJobConfig> GetByName(string jobName)
        {
            return SparkJobConfig.From(await this.Storage.GetByName(jobName, DataCollectionName));
        }

        private Task<Result> UpdateStringFieldByName(string jobName, string fieldName, string fieldValue)
        {
            return Storage.UpdatePartialByName(JsonConvert.SerializeObject(fieldValue), fieldName, jobName, DataCollectionName);
        }

        private Task<Result> UpdateJsonFieldByName(string jobName, string fieldName, JsonConfig fieldValue)
        {
            return Storage.UpdatePartialByName(fieldValue.ToString(), fieldName, jobName, DataCollectionName);
        }

        private Task<Result> UpdateObjectFieldByName(string jobName, string fieldName, object fieldValue)
        {
            return Storage.UpdatePartialByName(JsonConvert.SerializeObject(fieldValue), fieldName, jobName, DataCollectionName);
        }

        public Task<Result> UpdateSyncResultByName(string jobName,  SparkJobSyncResult result)
        {
            return UpdateObjectFieldByName(jobName, SparkJobConfig.FieldName_SyncResult, result);
        }

        public async Task<Result> DeleteByName(string jobName)
        {
            return await this.Storage.DeleteByName(jobName, DataCollectionName);
        }
    }
}
