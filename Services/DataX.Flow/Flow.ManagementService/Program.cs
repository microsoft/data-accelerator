// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Contract.Settings;
using DataX.ServiceHost;
using DataX.ServiceHost.ServiceFabric.Extensions.Configuration;
using DataX.Utilities.Telemetry;
using Flow.ManagementService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Flow.Management
{
    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                if (HostUtil.InServiceFabric)
                {
                    // The ServiceManifest.XML file defines one or more service type names.
                    // Registering a service maps a service type name to a .NET type.
                    // When Service Fabric creates an instance of this service type,
                    // an instance of the class is created in this host process.

                    ServiceRuntime.RegisterServiceAsync("Flow.ManagementServiceType",
                        context => new FlowManagementService(context, WebHostBuilder)).GetAwaiter().GetResult();

                    ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(FlowManagementService).Name);

                    // Prevents this host process from terminating so services keeps running. 
                    Thread.Sleep(Timeout.Infinite);
                }
                else
                {
                    var webHost = WebHostBuilder.Build();

                    webHost.Start();
                    webHost.WaitForShutdown();
                }
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
                throw;
            }
        }

        // Methods below are used for standalone deployment and is considered the default setup

        private static IWebHostBuilder WebHostBuilder
            => new WebHostBuilder()
                    .UseKestrel()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .ConfigureAppConfiguration(ConfigureAppConfiguration)
                    .ConfigureServices(ConfigureServices)
                    .Configure(Configure);

        private static void ConfigureAppConfiguration(WebHostBuilderContext context, IConfigurationBuilder builder)
        {
            var env = context.HostingEnvironment;

            builder = builder
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddServiceFabricSettings("Config", DataXSettingsConstants.DataX)
                .AddEnvironmentVariables();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var config = services.BuildServiceProvider().GetRequiredService<IConfiguration>();

            // Add DataX settings to be picked up automatically
            var settings = config.GetSection(DataXSettingsConstants.ServiceEnvironment).Get<DataXSettings>();

            services.AddSingleton(settings);

            // Configures AppInsights logging
            StartUpUtil.ConfigureServices(services, config);

            // Adds JWT Auth
            var bearerOptions = new JwtBearerOptions();

            config.GetSection("JwtBearerOptions").Bind(bearerOptions);

            services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Audience = bearerOptions.Audience;
                options.Authority = bearerOptions.Authority;
            });

            var startup = new FlowManagementServiceStartup();
            startup.ConfigureServices(services);

            // Add the FlowManagement Startup to be called _along with_ using Configure in the WebHostBuilder
            services.AddTransient<IStartupFilter>(_=>startup);
        }

        private static void Configure(IApplicationBuilder app)
        {
            var env = app.ApplicationServices.GetRequiredService<IHostingEnvironment>();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}
