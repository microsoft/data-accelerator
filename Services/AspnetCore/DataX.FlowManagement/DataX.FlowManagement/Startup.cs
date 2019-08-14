// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Flow.Management;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DataX.Config;
using DataX.Config.PublicService;
using DataX.Contract;

namespace DataX.FlowManagement
{
    public class Startup
    {

        private const string _MetricsHttpEndpointRelativeUri = "/api/data/upload";
        private readonly ILoggerFactory _loggerFactory;

        public Startup(IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();

            _loggerFactory = loggerFactory;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // Initialize the settings by getting the values from settings file
            InitConfigSettings();

            // Export the Config dependencies
            Type[] exportTypes = new Type[] { typeof(FlowOperation), typeof(RuntimeConfigGeneration), typeof(JobOperation) };
            services.AddMefExportsFromAssemblies(ServiceLifetime.Scoped, GetOneBoxModeDependencyAssemblies(), exportTypes, null, _loggerFactory, true);
            var result = InitTemplatesForLocal(services);
            Ensure.IsSuccessResult(result);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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

            app.UseMvc();
        }

        // Get the required settings to bootstrap the config gen
        private void InitConfigSettings()
        {
            var settings = new DataXSettings();
            Configuration.GetSection("DataX").Bind(settings);

            // Local settings
            var httpEndpoint = settings.MetricsHttpEndpoint.TrimEnd('/') + _MetricsHttpEndpointRelativeUri;
            InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_EnableOneBox, settings.EnableOneBox);
            InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_LocalRoot, settings.LocalRoot);
            InitialConfiguration.Set(DataX.Config.Local.LocalSparkClient.ConfigSettingName_SparkHomeFolder, settings.SparkHome);
            InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_ClusterName, "localCluster");
            InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_ServiceKeyVaultName, "local");
            InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_RuntimeKeyVaultName, "local");
            InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_MetricEventHubConnectionKey, "local");
            InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_ConfigFolderContainerPath, "");
            InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_ConfigFolderHost, new System.Uri(Environment.CurrentDirectory).AbsoluteUri);
            InitialConfiguration.Set(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_LocalMetricsHttpEndpoint, httpEndpoint);
        }

        private Result InitTemplatesForLocal(IServiceCollection services)
        {
            var configuration = new ContainerConfiguration().WithAssemblies(GetOneBoxModeDependencyAssemblies());
            using (var container = configuration.CreateContainer())
            {
                var localTemplateInitializer = container.GetExport<DataX.Config.Local.TemplateInitializer>();
                return localTemplateInitializer?.Initialize().Result;
            }
        }

        // Get all the dependencies needed to fulfill the ConfigGen
        // requirements for oneBox mode
        private IList<Assembly> GetOneBoxModeDependencyAssemblies()
        {
            return new List<Assembly>()
                {
                    typeof(DataX.Config.ConfigGenConfiguration).Assembly,
                    typeof(DataX.Config.Local.LocalDesignTimeStorage).Assembly
                };
        }
    }
}
