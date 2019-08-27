// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

using DataX.Contract;
using Microsoft.Azure.Cosmos.Table;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataX.Utilities.Storage
{
    /// <summary>
    /// Utility base class for CosmosTable
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public abstract class StorageTableBase<TEntity> where TEntity: TableEntity, new()
    {
        public StorageTableBase(CloudTable table)
        {
            this.Table = table;
        }

        protected CloudTable Table { get; }

        protected async Task<TEntity> RetrieveAsync(string partitionKey, string rowKey)
        {
            if (string.IsNullOrWhiteSpace(partitionKey) || string.IsNullOrWhiteSpace(rowKey))
            {
                return null;
            }

            return await this.Table.ExecuteAndGetResultAsync<TEntity>(TableOperation.Retrieve<TEntity>(partitionKey, rowKey))
                .ContinueOnAnyContext();
        }

        public async Task<TEntity> CreateWithRetryAsync(TEntity entity)
        {
            Ensure.NotNull(entity, nameof(entity));

            return await StorageUtils.RetryOnConflictAsync(
                async () =>
                {

                    await this.Table.InsertAsync(entity);
                    return entity;
                });
        }

        public async Task<IEnumerable<TEntity>> QueryAsync()
        {
            return await this.Table.CreateQuery<TEntity>().ExecuteQueryAsync();
        }

        public async Task<IEnumerable<TEntity>> RetrieveAllAsync()
        {
            var allEntities = new List<TEntity>();
            return await this.Table.ExecuteQueryAsync(new TableQuery<TEntity>());
        }

        public Task<TEntity> InsertOrReplace(TEntity entity)
        {
            return this.Table.InsertOrReplace(entity);
        }

        public Task<TEntity> InsertAsync(TEntity entity)
        {
            return this.Table.InsertAsync(entity);
        }

        public Task<TEntity> ReplaceAsync(TEntity entity)
        {
            return this.Table.ReplaceAsync(entity);
        }

        public Task DeleteAsync(TEntity entity)
        {
            return this.Table.DeleteAsync(entity);
        }
    }
}
