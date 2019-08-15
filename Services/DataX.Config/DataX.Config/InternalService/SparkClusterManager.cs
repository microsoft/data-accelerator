// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Contract;
using DataX.Contract.Exception;
using System;
using System.Collections.Concurrent;
using System.Composition;
using System.Threading.Tasks;

namespace DataX.Config
{
    [Shared]
    [Export]
    public class SparkClusterManager
    {
        [ImportingConstructor]
        public SparkClusterManager(SparkClusterData data, ISparkJobClientFactory clientFactory, IKeyVaultClient keyvaultClient)
        {
            ClusterData = data;
            ClientFactory = clientFactory;
            KeyVaultClient = keyvaultClient;
            _cache = new ConcurrentDictionary<string, ISparkJobClient>();
        }

        public SparkClusterData ClusterData { get; }
        public ISparkJobClientFactory ClientFactory { get; }
        public IKeyVaultClient KeyVaultClient { get; }

        //TODO: create a client pool to reduce the creation
        private readonly ConcurrentDictionary<string, ISparkJobClient> _cache;

        /// <summary>
        /// Get the Job client asynchronously
        /// </summary>
        /// <param name="clusterName"></param>
        /// <returns></returns>
        public async Task<ISparkJobClient> GetSparkJobClient(string clusterName, string databricksToken)
        {
            return await CreateSparkJobClient(clusterName, databricksToken);
        }

        /// <summary>
        /// Create the Job client asynchronously
        /// </summary>
        /// <param name="clusterName"></param>
        /// <returns></returns>
        private async Task<ISparkJobClient> CreateSparkJobClient(string clusterName, string databricksToken)
        {
            var cluster = await this.ClusterData.GetByNameAsync(clusterName);
            if (cluster == null)
            {
                throw new GeneralException($"cannot find cluster with name '{clusterName}'");
            }

            var connectionString = await this.KeyVaultClient.ResolveSecretUriAsync(cluster.ConnectionString);

            //If databricks token exist then append it to the connection string
            if(!string.IsNullOrWhiteSpace(databricksToken))
            {
                connectionString += await this.KeyVaultClient.ResolveSecretUriAsync(databricksToken);
            }

            return await this.ClientFactory.GetClient(connectionString);
        }
    }
}
