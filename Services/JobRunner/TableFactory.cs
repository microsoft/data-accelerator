// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.Azure.Cosmos.Table;
using DataX.Utilities.KeyVault;
using System.Composition;

namespace JobRunner
{
    [Export]
    [Shared]
    public class TableFactory
    {
        private readonly CloudTableClient _tableClient;

        [ImportingConstructor]
        public TableFactory(AppConfig config)
        {
            var storageConnection = KeyVault.GetSecretFromKeyvault(config.StorageConnection);
            var storageAccount = CloudStorageAccount.Parse(storageConnection);
            _tableClient = storageAccount.CreateCloudTableClient();
        }

        public CloudTable GetOrCreateTable(string tableName)
        {
            var table = _tableClient.GetTableReference(tableName);
            table.CreateIfNotExists();
            return table;
        }
    }
}
