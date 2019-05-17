// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config;
using DataX.Config.ConfigurationProviders;
using DataX.Config.PublicService;
using DataX.ServiceHost.AspNetCore.Authorization.Extensions;
using DataX.ServiceHost.ServiceFabric.Extensions.Configuration;
using DataX.Config.Storage;
using DataX.ServiceHost.ServiceFabric;
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
using DataX.Config.Local;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using DataX.ServiceHost.ServiceFabric.Authorization;
using DataX.Contract.Settings;

namespace Flow.Management
{
    public class Startup
    {
        private const string _MetricsHttpEndpointRelativeUri = "/api/data/upload";

        private readonly ILoggerFactory _loggerFactory;
        public IConfiguration Configuration { get; }
        private readonly DataXSettings _dataXSettings = new DataXSettings();

        public Startup(IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            // TODO: tylake - Not needed
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddServiceFabricSettings("Config", DataXSettingsConstants.DataX)
                .AddEnvironmentVariables();

            // TODO: tylake - Not needed
            Configuration = builder.Build();

            // TODO: tylake - Not needed
            Configuration.GetSection(DataXSettingsConstants.ServiceEnvironment).Bind(_dataXSettings);

            // TODO: tylake - Not needed
            _loggerFactory = loggerFactory;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
                                // TODO: tylake - Not needed
            services.AddMvc();//.SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // TODO: tylake - Not needed
            var bearerOptions = new JwtBearerOptions();

            // TODO: tylake - Not needed
            Configuration.GetSection("JwtBearerOptions").Bind(bearerOptions);

            services
                .AddSingleton(_dataXSettings)
                .AddDataXAuthorization(DataXDefaultGatewayPolicy.ConfigurePolicy);
            // TODO: tylake - Not needed
            //.AddAuthentication(options =>
            //{
            //    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            //    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            //    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            //})
            //.AddJwtBearer(options =>
            //{
            //    options.Audience = bearerOptions.Audience;
            //    options.Authority = bearerOptions.Authority;
            //});

            StartUpUtil.ConfigureServices(services, Configuration);

            // Initialize the settings by getting the values from settings file
            InitConfigSettings();

            // Export the Config dependencies
            Type[] exportTypes = new Type[] { typeof(FlowOperation), typeof(RuntimeConfigGeneration), typeof(JobOperation) };

            IEnumerable<Assembly> dependencyAssemblies = _dataXSettings.EnableOneBox ? OneBoxModeDependencyAssemblies : CloudModeDependencyAssemblies;
            IEnumerable<Assembly> additionalAssemblies = GetDependencyAssembliesFromStorageAsync().Result;

            var allAssemblies = dependencyAssemblies.Union(additionalAssemblies);

            services.AddMefExportsFromAssemblies(ServiceLifetime.Scoped, allAssemblies, exportTypes, null, _loggerFactory, _dataXSettings.EnableOneBox);
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

            //app.UseAuthentication();

            // Configure logger that will be injected into the controller
            app.UseMvc();
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
            InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_EnableOneBox, _dataXSettings.EnableOneBox.ToString());

            if (!_dataXSettings.EnableOneBox)
            {
                InitialConfiguration.Set(CosmosDbConfigurationProvider.ConfigSettingName_CosmosDBConfig_ConnectionString, _dataXSettings.CosmosDBConfigConnectionString);
                InitialConfiguration.Set(CosmosDbConfigurationProvider.ConfigSettingName_CosmosDBConfig_DatabaseName, _dataXSettings.CosmosDBConfigDatabaseName);
                InitialConfiguration.Set(CosmosDbConfigurationProvider.ConfigSettingName_CosmosDBConfig_CollectionName, _dataXSettings.CosmosDBConfigCollectionName);
                InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_ServiceKeyVaultName, _dataXSettings.ServiceKeyVaultName);
            }
            else
            {
                // Local settings
                var metricsHttpEndpoint = _dataXSettings.MetricsHttpEndpoint.TrimEnd('/') + _MetricsHttpEndpointRelativeUri;
                InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_LocalRoot, _dataXSettings.LocalRoot);
                InitialConfiguration.Set(DataX.Config.Local.LocalSparkClient.ConfigSettingName_SparkHomeFolder, _dataXSettings.SparkHome);
                InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_ClusterName, "localCluster");
                InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_ServiceKeyVaultName, "local");
                InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_RuntimeKeyVaultName, "local");
                InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_MetricEventHubConnectionKey, "local");
                InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_ConfigFolderContainerPath, "");
                InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_ConfigFolderHost, new System.Uri(Environment.CurrentDirectory).AbsoluteUri);
                InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_LocalMetricsHttpEndpoint, metricsHttpEndpoint);
            }

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
