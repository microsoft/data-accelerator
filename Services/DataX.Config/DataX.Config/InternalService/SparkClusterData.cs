// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using System;
using System.Composition;
using System.Threading.Tasks;

namespace DataX.Config
{
    [Shared]
    [Export]
    public class SparkClusterData
    {
        public const string DataCollectionName = "sparkClusters";

        [ImportingConstructor]
        public SparkClusterData([Import] IDesignTimeConfigStorage storage)
        {
            this.Storage = storage;
        }

        private IDesignTimeConfigStorage Storage { get; }

        public async Task<SparkClusterConfig> GetByNameAsync(string clusterName)
        {
            return SparkClusterConfig.From(JsonConfig.From(await Storage.GetByName(clusterName, DataCollectionName)));
        }
    }
}
