// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Contract;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.Test.Mock
{
    [Shared]
    [Export]
    [Export(typeof(IDesignTimeConfigStorage))]
    internal class DesignTimeStorage : IDesignTimeConfigStorage
    {
        private readonly Dictionary<string, string> _cache = new Dictionary<string, string>();

        private string GetKeyName(string entityName, string collectionName)
        {
            return $"{collectionName}-{entityName}";
        }

        public async Task<string> GetEmptyJsonConfig()
        {
            await Task.Yield();
            return "{}";
        }

        public async Task<string[]> GetEmptyJsonConfigs()
        {
            await Task.Yield();
            return Array.Empty<string>();
        }

        public async Task<Result> GetSuccessResult()
        {
            await Task.Yield();
            return new SuccessResult();
        }

        public async Task<string[]> GetAll(string collectionName)
        {
            return await this.GetEmptyJsonConfigs();
        }

        public async Task<string> GetByName(string name, string collectionName)
        {
            await Task.Yield();
            return this._cache.GetValueOrDefault(this.GetKeyName(name, collectionName));
        }

        public async Task<string[]> GetByNames(string[] names, string collectionName)
        {
            return await this.GetEmptyJsonConfigs();
        }

        public async Task<Result> SaveByName(string name, string content, string collectionName)
        {
            this._cache.Add(this.GetKeyName(name, collectionName), content);
            return await this.GetSuccessResult();
        }

        public async Task<Result> UpdatePartialByName(string partial, string field, string name, string collectionName)
        {
            var key = this.GetKeyName(name, collectionName);
            var baseConfig = this._cache[key];
            JsonConfig fieldData = JsonConfig.From(partial);
            var builder = JsonConfig.StartBuild().WithBase(JsonConfig.From(baseConfig));
            var finalBuilder = builder.ReplaceFieldWithConfig(field, fieldData);
            var mergedConfig = finalBuilder.Build();
            this._cache[key] = mergedConfig.ToString();

            return await this.GetSuccessResult();
        }

        public Task<string[]> GetByFieldValue(string matchValue, string field, string collectionName)
        {
            throw new NotImplementedException();
        }

        public async Task<Result> DeleteByName(string name, string collectionName)
        {
            this._cache.Remove(this.GetKeyName(name, collectionName));
            return await this.GetSuccessResult();
        }
    }
}
