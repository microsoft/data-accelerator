using DataX.Config;
using DataX.Config.ConfigurationProviders;
using DataX.Config.PublicService;
using DataX.Contract.Settings;
using DataX.ServiceHost.AspNetCore.Startup;
using DataX.Utilities.Blob;
using Flow.Management;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Flow.ManagementService
{
    public sealed class FlowManagementServiceStartup : DataXServiceStartup
    {
        private const string _MetricsHttpEndpointRelativeUri = "/api/data/upload";

        public FlowManagementServiceStartup() { }

        public FlowManagementServiceStartup(DataXSettings settings)
            : base(settings) { }

        /// <inheritdoc />
        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            // Initialize the settings by getting the values from settings file
            InitConfigSettings();

            var loggerFactory = services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();

            // Export the Config dependencies
            Type[] exportTypes = new Type[] { typeof(FlowOperation), typeof(RuntimeConfigGeneration), typeof(JobOperation) };

            IEnumerable<Assembly> dependencyAssemblies = Settings.EnableOneBox ? OneBoxModeDependencyAssemblies : CloudModeDependencyAssemblies;
            IEnumerable<Assembly> additionalAssemblies = GetDependencyAssembliesFromStorageAsync().Result;

            var allAssemblies = dependencyAssemblies.Union(additionalAssemblies);

            services.AddMefExportsFromAssemblies(ServiceLifetime.Scoped, allAssemblies, exportTypes, null, loggerFactory, Settings.EnableOneBox);
        }

        // Get all the dependencies needed to fulfill the ConfigGen
        // requirements for cloud mode
        private IList<Assembly> CloudModeDependencyAssemblies
            => new List<Assembly>()
                {
                    typeof(DataX.Config.ConfigGenConfiguration).Assembly,
                    typeof(DataX.Config.ConfigurationProviders.CosmosDbConfigurationProvider).Assembly,
                    typeof(DataX.Config.Storage.CosmosDBConfigStorage).Assembly,
                    typeof(DataX.Config.KeyVault.KeyVaultClient).Assembly,
                    typeof(DataX.Config.LivyClient.LivyClientFactory).Assembly,
                    typeof(DataX.Config.Input.EventHub.Processor.CreateEventHubConsumerGroup).Assembly
                };

        // Get all the dependencies needed to fulfill the ConfigGen
        // requirements for oneBox mode
        private IList<Assembly> OneBoxModeDependencyAssemblies
            => new List<Assembly>()
                {
                    typeof(DataX.Config.ConfigGenConfiguration).Assembly,
                    typeof(DataX.Config.Local.LocalDesignTimeStorage).Assembly
                };

        // Get the required settings to bootstrap the config gen
        private void InitConfigSettings()
        {
            InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_EnableOneBox, Settings.EnableOneBox.ToString());

            if (!Settings.EnableOneBox)
            {
                InitialConfiguration.Set(CosmosDbConfigurationProvider.ConfigSettingName_CosmosDBConfig_ConnectionString, Settings.CosmosDBConfigConnectionString);
                InitialConfiguration.Set(CosmosDbConfigurationProvider.ConfigSettingName_CosmosDBConfig_DatabaseName, Settings.CosmosDBConfigDatabaseName);
                InitialConfiguration.Set(CosmosDbConfigurationProvider.ConfigSettingName_CosmosDBConfig_CollectionName, Settings.CosmosDBConfigCollectionName);
                InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_ServiceKeyVaultName, Settings.ServiceKeyVaultName);
            }
            else
            {
                // Local settings
                var metricsHttpEndpoint = Settings.MetricsHttpEndpoint.TrimEnd('/') + _MetricsHttpEndpointRelativeUri;
                InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_LocalRoot, Settings.LocalRoot);
                InitialConfiguration.Set(DataX.Config.Local.LocalSparkClient.ConfigSettingName_SparkHomeFolder, Settings.SparkHome);
                InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_ClusterName, "localCluster");
                InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_ServiceKeyVaultName, "local");
                InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_RuntimeKeyVaultName, "local");
                InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_MetricEventHubConnectionKey, "local");
                InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_ConfigFolderContainerPath, "");
                InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_ConfigFolderHost, new Uri(Environment.CurrentDirectory).AbsoluteUri);
                InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_LocalMetricsHttpEndpoint, metricsHttpEndpoint);
            }

        }

        // Get additional assemblies from azure storage
        private async Task<IEnumerable<Assembly>> GetDependencyAssembliesFromStorageAsync()
        {
            IEnumerable<Assembly> additionalAssemblies = new List<Assembly>();
            var mefStorageAccountName = Settings.MefStorageAccountName;
            var mefContainerName = Settings.MefContainerName;

            if (string.IsNullOrEmpty(mefStorageAccountName) || string.IsNullOrEmpty(mefContainerName))
            {
                return additionalAssemblies;
            }

            var mefBlobDirectory = Settings.MefBlobDirectory;

            BlobStorageMSI blobStorage = new BlobStorageMSI(mefStorageAccountName);

            var dlls = blobStorage.GetCloudBlockBlobs(mefContainerName, mefBlobDirectory);

            foreach (var blob in dlls)
            {
                if (blob.Name.EndsWith(".dll"))
                {
                    using (var strm = new MemoryStream())
                    {
                        await blob.DownloadToStreamAsync(strm);
                        byte[] asseblyBytes = strm.ToArray();
                        var assembly = Assembly.Load(asseblyBytes);
                        additionalAssemblies = additionalAssemblies.Append(assembly);
                    }
                }
            }
            return additionalAssemblies;
        }
    }
}
