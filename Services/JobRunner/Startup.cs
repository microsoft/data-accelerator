// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using DataX.Utilities.KeyVault;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;

namespace JobRunner
{
    internal class Startup
    {
        public IConfiguration Configuration { get; }        

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services, AppConfig settings)
        {
            services
                .AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions()
                {
                    EnableAdaptiveSampling = false,
                    EnableDebugLogger = false,
                    InstrumentationKey = GetInstrumentationKey(settings)
                })
                .AddLogging(logging =>
                {
                    try
                    {
                        // In order to log ILogger logs
                        logging.AddApplicationInsights();
                        // Optional: Apply filters to configure LogLevel Information or above is sent to
                        // ApplicationInsights for all categories.
                        logging.AddFilter<ApplicationInsightsLoggerProvider>("", LogLevel.Information);

                        // Additional filtering For category starting in "Microsoft",
                        // only Warning or above will be sent to Application Insights.
                        logging.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft", LogLevel.Warning);

                    }
                    catch (Exception e)
                    {                        
                    }
                });
            
        }


        public void Configure(IApplicationBuilder app)
        {
            var hostingEnvironment = app.ApplicationServices.GetService<IHostingEnvironment>();
            var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>();

            Configure(app, hostingEnvironment, loggerFactory);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }            

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }

        /// <summary>
        /// Helper method for getting the ApplicationInsights key from the keyvault
        /// </summary>
        /// <param name="settings">This is an object that contains the appsettings values as set in the appsettings.json</param>
        /// <returns></returns>
        private static string GetInstrumentationKey(AppConfig settings)
        {
            var secretName = settings?.AppInsightsIntrumentationKey;
            var vaultName = settings?.ServiceKeyVaultName;

            return string.IsNullOrWhiteSpace(secretName) || string.IsNullOrWhiteSpace(vaultName)
                ? Guid.Empty.ToString()
                : KeyVault.GetSecretFromKeyvault(settings.ServiceKeyVaultName, settings.AppInsightsIntrumentationKey);
        }
    }
}
