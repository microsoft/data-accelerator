// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

using DataX.Contract;
using DataX.Utilities.Storage;
using System;
using System.Composition;
using System.Threading.Tasks;

namespace JobRunner
{
    /// <summary>
    /// CosmosTable will be storing the scheduled jobs information like the start running at time.
    /// </summary>
    [Export]
    [Shared]    
    public class ScheduledJobTable : StorageTableBase<ScheduledJobEntity>
    {
        [ImportingConstructor]
        public ScheduledJobTable(TableFactory tableFactory) :
            base(tableFactory.GetOrCreateTable("scheduledJobs"))
        {
        }

        public Task<ScheduledJobEntity> RetrieveAsync(string name)
        {
            return base.RetrieveAsync(ScheduledJobEntity.SharedPartitionKey, name);
        }

        public async Task<ScheduledJobEntity> CreateOrUpdateScheduledJobAsync(string name, string jobKey, DateTimeOffset startAt, TimeSpan interval, bool useTestQueue = false)
        {
            Ensure.Equals(string.IsNullOrWhiteSpace(jobKey), false);
            Ensure.Equals(string.IsNullOrWhiteSpace(name), false);
            var newSchedule = new ScheduledJobEntity(name, jobKey, startAt, interval, useTestQueue);

            return await StorageUtils.RetryOnConflictAsync<ScheduledJobEntity>(
                async () =>
                {
                    try
                    {
                        var existingSchedule = await this.RetrieveAsync(name);
                        newSchedule.LastRunAt = existingSchedule.LastRunAt;
                    }
                    catch { }
                    return await this.Table.InsertOrReplace(newSchedule).ContinueOnAnyContext();
                }
                );
        }
    }
}
