// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.ServiceHost.ServiceFabric;
using DataX.Contract.Settings;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using System;

namespace DataX.Utilities.Telemetry
{
    public static class StartUpUtil
    {
        public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var settings = configuration.GetSection(DataXSettingsConstants.ServiceEnvironment).Get<DataXSettings>();

            ConfigureServices(services, settings ?? new DataXSettings());
        }

        public static void ConfigureServices(IServiceCollection services, DataXSettings settings)
        {
            services
                .AddSingleton<ITelemetryInitializer, OperationParentIdTelemetryInitializer>()
                .AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions()
                {
                    EnableAdaptiveSampling = false,
                    EnableDebugLogger = false,
                    InstrumentationKey = GetInstrumentationKey(settings)
                })
                .AddSingleton<ITelemetryInitializer, OperationParentIdTelemetryInitializer>()
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
                        ServiceEventSource.Current.Message($"ApplicationInsights Error: {e.Message}");
                    }
                });
        }

        private static string GetInstrumentationKey(DataXSettings settings)
        {
            var secretName = settings?.AppInsightsIntrumentationKey;
            var vaultName = settings.ServiceKeyVaultName;

            return string.IsNullOrWhiteSpace(secretName) || string.IsNullOrWhiteSpace(vaultName)
                ? Guid.Empty.ToString()
                : KeyVault.KeyVault.GetSecretFromKeyvault(settings.ServiceKeyVaultName, settings.AppInsightsIntrumentationKey);
        }
    }
}
