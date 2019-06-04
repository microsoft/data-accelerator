// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
namespace DataX.ServiceHost.AspNetCore.Extensions
{
    using DataX.Contract.Settings;
    using DataX.ServiceHost.AspNetCore.Startup;
    using DataX.ServiceHost.ServiceFabric.Extensions.Configuration;
    using DataX.Utilities.Telemetry;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using System.IO;

    /// <summary>
    /// Extensions for IWebHostBuilder
    /// </summary>
    public static class DataXWebHostBuilderExtensions
    {
        /// <summary>
        /// Configures the WebHostBuilder with DataX defaults.
        /// </summary>
        /// <param name="hostBuilder">The host builder to be configured</param>
        /// <returns>The configured host builder</returns>
        public static IWebHostBuilder UseDataXDefaultConfiguration(this IWebHostBuilder hostBuilder)
        {
            return hostBuilder
                    .UseKestrel()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseDataXDefaultAppConfiguration()
                    .UseDataXDefaultConfigureServices();
        }

        /// <summary>
        /// Configures the WebHostBuilder with DataX defaults and a DataX startup filter.
        /// </summary>
        /// <typeparam name="TStartup">DataX concrete startup filter type inheriting from <see cref="DataXServiceStartup"/></typeparam>
        /// <param name="hostBuilder">The host builder to be configured</param>
        /// <param name="startup">Optional startup instance to be used.</param>
        /// <returns>The configured host builder</returns>
        public static IWebHostBuilder UseDataXDefaultConfiguration<TStartup>(this IWebHostBuilder hostBuilder, TStartup startup = null)
            where TStartup : DataXServiceStartup, new()
        {
            return hostBuilder
                    .UseDataXDefaultConfiguration()
                    .UseDataXStartup(startup);
        }

        /// <summary>
        /// Configures the WebHostBuilder with the DataX default IConfigurationBuilder settings.
        /// </summary>
        /// <param name="hostBuilder">The host builder to be configured</param>
        /// <returns>The configured host builder</returns>
        public static IWebHostBuilder UseDataXDefaultAppConfiguration(this IWebHostBuilder hostBuilder)
        {
            void ConfigureAppConfiguration(WebHostBuilderContext context, IConfigurationBuilder builder)
            {
                var env = context.HostingEnvironment;

                builder = builder
                    .SetBasePath(env.ContentRootPath)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                    .AddServiceFabricSettings("Config", DataXSettingsConstants.DataX)
                    .AddEnvironmentVariables();
            }

            return hostBuilder.ConfigureAppConfiguration(ConfigureAppConfiguration);
        }

        /// <summary>
        /// Configures the WebHostBuilder with the DataX default services.
        /// </summary>
        /// <param name="hostBuilder">The host builder to be configured</param>
        /// <returns>The configured host builder</returns>
        public static IWebHostBuilder UseDataXDefaultConfigureServices(this IWebHostBuilder hostBuilder)
        {
            void ConfigureServices(IServiceCollection services)
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
            }

            return hostBuilder.ConfigureServices(ConfigureServices);
        }

        /// <summary>
        /// Adds a DataX startup filter to a WebHostBuilder.
        /// </summary>
        /// <typeparam name="TStartup">DataX concrete startup filter type inheriting from <see cref="DataXServiceStartup"/></typeparam>
        /// <param name="hostBuilder">The host builder to be configured</param>
        /// <param name="startup">Optional startup instance to be used.</param>
        /// <returns>The configured host builder</returns>
        public static IWebHostBuilder UseDataXStartup<TStartup>(this IWebHostBuilder hostBuilder, TStartup startup = null)
            where TStartup : DataXServiceStartup, new()
        {
            return hostBuilder.ConfigureServices(services => services.AddDataXStartup(startup));
        }
    }
}
