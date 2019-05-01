// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config;
using DataX.Config.ConfigurationProviders;
using DataX.Config.PublicService;
using DataX.ServiceHost.ServiceFabric.Extensions.Configuration;
using DataX.Config.Storage;
using DataX.ServiceHost.ServiceFabric;
using DataX.ServiceHost.Settings;
using DataX.Utilities.Blob;
using DataX.Utilities.Telemetry;
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

namespace Flow.Management
{
    public class Startup
    {
        private readonly ILoggerFactory _loggerFactory;
        public IConfiguration Configuration { get; }
        private readonly DataXSettings _dataXSettings = new DataXSettings();

        public Startup(IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddServiceFabricSettings("Config")
                .AddEnvironmentVariables();

            Configuration = builder.Build();

            Configuration.GetSection(DataXSettingsConstants.ServiceEnvironment).Bind(_dataXSettings);

            _loggerFactory = loggerFactory;
        }        
      
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            StartUpUtil.ConfigureServices(services, Configuration);

            // Configure and create a logger instance to add it to MEF container           
            var logger = _loggerFactory.CreateLogger<RuntimeConfigGeneration>();

            // Initialize the settings by getting the values from settings file
            InitConfigSettings();

            // Export the Config dependencies
            Type[] exportTypes = new Type[] { typeof(FlowOperation), typeof(RuntimeConfigGeneration), typeof(JobOperation) };

            IEnumerable<Assembly> cloudModeDependencyAssemblies = GetCloudModeDependencyAssemblies();
            IEnumerable<Assembly> additionalAssemblies = GetDependencyAssembliesFromStorageAsync().Result;

            var allAssemblies = cloudModeDependencyAssemblies.Union(additionalAssemblies);

            services.AddMefExportsFromAssemblies(ServiceLifetime.Scoped, allAssemblies, exportTypes, new object[] { logger });            
        }      

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Set content-type options header to honor the server's mimetype
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("X-Content-Type-Options", new string[] { "nosniff" });
                await next();
            });

            // Configure logger that will be injected into the controller
            app.UseMvc();
        }

        // Get all the dependencies needed to fulfill the ConfigGen
        // requirements for cloud mode
        private IList<Assembly> GetCloudModeDependencyAssemblies()
        {
            return new List<Assembly>()
                {
                    typeof(DataX.Config.ConfigGenConfiguration).Assembly,
                    typeof(DataX.Config.ConfigurationProviders.CosmosDbConfigurationProvider).Assembly,
                    typeof(DataX.Config.Storage.CosmosDBConfigStorage).Assembly,
                    typeof(DataX.Config.KeyVault.KeyVaultClient).Assembly,
                    typeof(DataX.Config.LivyClient.LivyClientFactory).Assembly,
                    typeof(DataX.Config.Input.EventHub.Processor.CreateEventHubConsumerGroup).Assembly
                };
        }

        // Get all the dependencies needed to fulfill the ConfigGen
        // requirements for oneBox mode
        private IList<Assembly> GetOneBoxModeDependencyAssemblies()
        {
            throw new NotImplementedException();
        }

        // Get the required settings to bootstrap the config gen
        private void InitConfigSettings()
        {
            InitialConfiguration.Set(CosmosDbConfigurationProvider.ConfigSettingName_CosmosDBConfig_ConnectionString, _dataXSettings.CosmosDBConfigConnectionString);
            InitialConfiguration.Set(CosmosDbConfigurationProvider.ConfigSettingName_CosmosDBConfig_DatabaseName, _dataXSettings.CosmosDBConfigDatabaseName);
            InitialConfiguration.Set(CosmosDbConfigurationProvider.ConfigSettingName_CosmosDBConfig_CollectionName, _dataXSettings.CosmosDBConfigCollectionName);
            InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_ServiceKeyVaultName, _dataXSettings.ServiceKeyVaultName);
            InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_EnableOneBox, _dataXSettings.EnableOneBox.ToString());
        }

        // Get additional assemblies from azure storage
        private async Task<IEnumerable<Assembly>> GetDependencyAssembliesFromStorageAsync()
        {
            IEnumerable<Assembly> additionalAssemblies = new List<Assembly>();
            var mefStorageAccountName = _dataXSettings.MefStorageAccountName;
            var mefContainerName = _dataXSettings.MefContainerName;

            if (string.IsNullOrEmpty(mefStorageAccountName) || string.IsNullOrEmpty(mefContainerName))
            {
                return additionalAssemblies;
            }

            var mefBlobDirectory = _dataXSettings.MefBlobDirectory;

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
