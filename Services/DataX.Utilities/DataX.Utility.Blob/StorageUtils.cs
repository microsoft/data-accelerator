// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.File;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace DataX.Utilities.Storage
{
    public static class StorageUtils
    {
        /// <summary>
        /// Calls an async function, and if it fails due to a table storage precondition
        /// failure will attempt to retry the operation.
        /// </summary>
        /// <param name="a">The function to be retried on failure</param>
        /// <param name="conflict">
        /// An optional notification that only gets called on a conflict.
        /// Return true if after running the conflict resolution task we need
        /// to retry. Return false if retry is not necessary.
        /// </param>
        public static async Task RetryOnConflictAsync(Func<Task> a, Func<Task<bool>> conflict = null)
        {
            while (true)
            {
                try
                {
                    await a().ContinueOnAnyContext();
                    break;
                }
                catch (Microsoft.Azure.Cosmos.Table.StorageException se)
                {
                    if (se.RequestInformation.HttpStatusCode != (int)HttpStatusCode.PreconditionFailed)
                    {
                        throw;
                    }
                    if (conflict != null)
                    {
                        if (!await conflict.Invoke().ContinueOnAnyContext())
                        {
                            return;
                        }
                    }
                }
            }
        }
        public static async Task<T> RetryOnConflictAsync<T>(Func<Task<T>> a, Func<Task<bool>> conflict = null)
        {
            while (true)
            {
                try
                {
                    return await a().ContinueOnAnyContext();
                }
                catch (Microsoft.Azure.Cosmos.Table.StorageException se)
                {
                    if (se.RequestInformation.HttpStatusCode != (int)HttpStatusCode.PreconditionFailed)
                    {
                        throw;
                    }
                    if (conflict != null)
                    {
                        if (!await conflict.Invoke().ContinueOnAnyContext())
                        {
                            return default(T);
                        }
                    }
                }
            }
        }



        public static async Task<bool> CreateIfNotExistsSafeAsync(this CloudBlobContainer container)
        {
            return await DynamicCreateIfNotExistsSafeAsync(container).ContinueOnAnyContext();
        }

        public static async Task<bool> CreateIfNotExistsSafeAsync(this CloudTable cloudTable)
        {
            return await DynamicCreateIfNotExistsSafeAsync(cloudTable).ContinueOnAnyContext();
        }

        public static async Task<bool> CreateIfNotExistsSafeAsync(this CloudFileShare cloudFileShare)
        {
            return await DynamicCreateIfNotExistsSafeAsync(cloudFileShare).ContinueOnAnyContext();
        }

        /// <summary>
        /// Wraps CreateIfNotExistsAsync in a backoff timer. Could take 2
        /// minutes of waiting until safe to recreate. This handles an issue
        /// where the table in a middle of a deletion will throw.
        /// </summary>
        /// <param name="storageType">dynamic for storage types with a CreateIfNotExistsAsync method</param>
        /// <returns>True if created, False otherwise</returns>
        private static async Task<bool> DynamicCreateIfNotExistsSafeAsync(dynamic storageType)
        {
            var succeeded = false;
            var result = false;
            StorageException last_exc = null;
            for (int tries = 0; tries < 15; tries++)
            {
                try
                {
                    // create if not exist can fail if it is in the process of being deleted. If we receive a 409 conflict, we should sleep and try again.
                    result = await storageType.CreateIfNotExistsAsync().ConfigureAwait(continueOnCapturedContext: false);
                    succeeded = true;
                }
                catch (StorageException e) when (e.Message == "The remote server returned an error: (409) Conflict.")
                {
                    Debug.Write("While trying to create we hit a conflict. Likely a deletion of this is in progress");
                    await Task.Delay(TimeSpan.FromSeconds(1 * tries)).ContinueOnAnyContext();
                    last_exc = e;
                }
                if (succeeded)
                {
                    return result;
                }
            }
            throw last_exc;
        }
    }
}
