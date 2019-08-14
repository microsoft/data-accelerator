// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using DataX.Contract.Settings;
using DataX.ServiceHost.AspNetCore.Authorization.Extensions;
using DataX.ServiceHost.ServiceFabric.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace DataX.ServiceHost.AspNetCore.Startup
{
    /// <summary>
    /// A basic abstract implementation of DataX startup to take care of common configuration.
    /// </summary>
    public abstract class DataXServiceStartup : IDataXServiceStartup
    {
        protected DataXSettings Settings { get; private set; }

        public DataXServiceStartup() { }

        public DataXServiceStartup(DataXSettings settings)
        {
            Settings = settings;
        }

        /// <inheritdoc />
        public virtual void ConfigureServices(IServiceCollection services)
        {
            if (Settings == null)
            {
                var provider = services.BuildServiceProvider();
                Settings = provider.GetService<DataXSettings>();

                // Let's look for the default settings
                if (Settings == null)
                {
                    Settings = provider.GetService<IConfiguration>()?.GetSection(DataXSettingsConstants.ServiceEnvironment).Get<DataXSettings>();

                    if (Settings != null)
                    {
                        services.TryAddSingleton(Settings);
                    }
                }
            }
            else
            {
                services.TryAddSingleton(Settings);
            }

            services
                .AddDataXAuthorization(DataXDefaultGatewayPolicy.ConfigurePolicy)
                .AddMvc();
        }

        /// <inheritdoc />
        public void Configure(IApplicationBuilder app)
        {
            var hostingEnvironment = app.ApplicationServices.GetService<IHostingEnvironment>();
            var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>();

            Configure(app, hostingEnvironment, loggerFactory);
        }

        /// <inheritdoc cref="DataXServiceStartup.Configure(IApplicationBuilder)" />
        protected virtual void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("X-Content-Type-Options", new string[] { "nosniff" });
                await next();
            });
            app.UseAuthentication();
            app.UseMvc();
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                Configure(app);
                next(app);
            };
        }
    }
}
