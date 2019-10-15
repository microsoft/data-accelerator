// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataX.Utilities.Storage
{
    /// <summary>
    /// CosmosTable utility class
    /// </summary>
    public static class TableExtensions
    {
        public static async Task<T> ExecuteAndGetResultAsync<T>(this CloudTable table, TableOperation operation)
        {
            var result = (T)(await table.ExecuteAsync(operation).ContinueOnAnyContext()).Result;
            return result;
        }

        public static async Task<IList<T>> ExecuteQueryAsync<T>(this CloudTable table, TableQuery<T> query, CancellationToken ct = default(CancellationToken)) where T : ITableEntity, new()
        {
            var items = new List<T>();
            TableContinuationToken token = null;

            do
            {
                TableQuerySegment<T> seg = await table.ExecuteQuerySegmentedAsync<T>(query, token).ContinueOnAnyContext();
                token = seg.ContinuationToken;
                items.AddRange(seg);
            } while (token != null && !ct.IsCancellationRequested && (query.TakeCount == null || items.Count < query.TakeCount.Value));

            return items;
        }

        public static async Task ForEachQueryResultAsync<T>(this CloudTable table, TableQuery<T> query, Action<T> processResult, CancellationToken ct = default(CancellationToken)) where T : ITableEntity, new()
        {
            TableContinuationToken token = null;
            int count = 0;
            do
            {
                TableQuerySegment<T> seg = await table.ExecuteQuerySegmentedAsync<T>(query, token).ContinueOnAnyContext();
                token = seg.ContinuationToken;
                foreach (var result in seg)
                {
                    processResult(result);
                    count++;
                }
            } while (token != null && !ct.IsCancellationRequested && (query.TakeCount == null || count < query.TakeCount.Value));
        }

        public static async Task ForEachQueryResultAsync<T>(this CloudTable table, TableQuery<T> query, Func<T, Task> processResultAsync, CancellationToken ct = default(CancellationToken)) where T : ITableEntity, new()
        {
            TableContinuationToken token = null;
            int count = 0;
            do
            {
                TableQuerySegment<T> seg = await table.ExecuteQuerySegmentedAsync<T>(query, token).ContinueOnAnyContext();
                token = seg.ContinuationToken;
                foreach (var result in seg)
                {
                    await processResultAsync(result).ContinueOnAnyContext();
                    count++;
                }
            } while (token != null && !ct.IsCancellationRequested && (query.TakeCount == null || count < query.TakeCount.Value));
        }

        public static async Task<TableContinuationToken> ForEachQueryResultAsync<T>(this CloudTable table, TableQuery<T> query, TableContinuationToken token, Func<T, Task> processResultAsync, CancellationToken ct = default(CancellationToken)) where T : ITableEntity, new()
        {
            int count = 0;
            do
            {
                TableQuerySegment<T> seg = await table.ExecuteQuerySegmentedAsync<T>(query, token).ContinueOnAnyContext();
                token = seg.ContinuationToken;
                foreach (var result in seg)
                {
                    await processResultAsync(result).ContinueOnAnyContext();
                    count++;
                }
            } while (token != null && !ct.IsCancellationRequested && (query.TakeCount == null || count < query.TakeCount.Value));
            return token;
        }

        public static async Task<IList<T>> ExecuteQueryAsync<T>(this TableQuery<T> query, CancellationToken ct = default(CancellationToken)) where T : ITableEntity, new()
        {
            var items = new List<T>();
            TableContinuationToken token = null;

            do
            {
                TableQuerySegment<T> seg = await query.ExecuteSegmentedAsync(token).ContinueOnAnyContext();
                token = seg.ContinuationToken;
                items.AddRange(seg);
            } while (token != null && !ct.IsCancellationRequested && (query.TakeCount == null || items.Count < query.TakeCount.Value));

            return items;
        }

        /// <summary>
        /// Executes a table query and processes the results as they arrive
        /// </summary>
        public static async Task ForEachQueryResultAsync<T>(this TableQuery<T> query, Func<T, Task> processResultAsync, CancellationToken ct = default(CancellationToken)) where T : ITableEntity, new()
        {
            TableContinuationToken token = null;

            int count = 0;
            do
            {
                TableQuerySegment<T> seg = await query.ExecuteSegmentedAsync(token).ContinueOnAnyContext();
                token = seg.ContinuationToken;
                foreach (var result in seg)
                {
                    await processResultAsync(result).ContinueOnAnyContext();
                    count++;
                }
            } while (token != null && !ct.IsCancellationRequested && (query.TakeCount == null || count < query.TakeCount.Value));
        }


        /// <summary>
        /// Executes a table query and processes the results as they arrive
        /// </summary>
        public static async Task ForEachQueryResultAsync<T>(this TableQuery<T> query, Func<T, Task<bool>> processResultAsync, CancellationToken ct = default(CancellationToken)) where T : ITableEntity, new()
        {
            TableContinuationToken token = null;

            int count = 0;
            do
            {
                TableQuerySegment<T> seg = await query.ExecuteSegmentedAsync(token).ContinueOnAnyContext();
                token = seg.ContinuationToken;
                foreach (var result in seg)
                {
                    if (!await processResultAsync(result).ContinueOnAnyContext())
                    {
                        break;
                    }
                    count++;
                }
            } while (token != null && !ct.IsCancellationRequested && (query.TakeCount == null || count < query.TakeCount.Value));
        }

        public static async Task<T> InsertAsync<T>(this CloudTable table, T entity) where T : TableEntity
        {
            return (T)(await table.ExecuteAsync(TableOperation.Insert(entity)).ContinueOnAnyContext()).Result;
        }

        public static async Task<T> InsertOrReplace<T>(this CloudTable table, T entity) where T : TableEntity
        {
            return (T)(await table.ExecuteAsync(TableOperation.InsertOrReplace(entity)).ContinueOnAnyContext()).Result;
        }

        public static Task DeleteAsync(this CloudTable table, TableEntity entity)
        {
            return table.ExecuteAsync(TableOperation.Delete(entity));
        }

        public static async Task<T> ReplaceAsync<T>(this CloudTable table, T entity) where T : TableEntity
        {
            return (T)(await table.ExecuteAsync(TableOperation.Replace(entity)).ContinueOnAnyContext()).Result;
        }
    }
}
