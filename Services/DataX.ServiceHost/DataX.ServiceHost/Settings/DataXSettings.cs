

namespace DataX.ServiceHost.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class DataXSettings
    {
        // The flat settings should be better split into other settings classes
        // For now, to keep compatibility with ServiceFabric AppManifest, this is flat

        public string CosmosDBConfigConnectionString { get; set; }
        public string CosmosDBConfigDatabaseName { get; set; }
        public string CosmosDBConfigCollectionName { get; set; }

        // DataX Settings
        public bool EnableOneBox { get; set; }
        public string LocalRoot { get; set; }
        public string SparkHome { get; set; }
        public string ClusterName { get; set; }
        public string ServiceKeyVaultName { get; set; }
        public string RuntimeKeyVaultName { get; set; }
        public string MetricEventHubConnectionKey { get; set; }
        public string ConfigFolderContainerPath { get; set; }
        public string ConfigFolderHost { get; set; }
        public string MetricsHttpEndpoint { get; set; }
        public string AppInsightsIntrumentationKey { get; set; }

        public string MefStorageAccountName { get; set; }
        public string MefContainerName { get; set; }
        public string MefBlobDirectory { get; set; }
    }
}
