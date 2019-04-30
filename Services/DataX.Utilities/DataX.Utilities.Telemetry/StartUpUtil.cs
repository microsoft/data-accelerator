// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.ServiceHost.ServiceFabric;
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
            services.AddSingleton<ITelemetryInitializer, OperationParentIdTelemetryInitializer>();
            services.AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions()
            {
                EnableAdaptiveSampling = false,
                EnableDebugLogger = false,
                InstrumentationKey = KeyVault.KeyVault.GetSecretFromKeyvault(ServiceFabricUtil.GetServiceKeyVaultName().Result.ToString(), ServiceFabricUtil.GetServiceFabricConfigSetting("AppInsightsIntrumentationKey").Result.ToString())
            });
            services.AddSingleton<ITelemetryInitializer, OperationParentIdTelemetryInitializer>();
            services.AddLogging(logging =>
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
    }
}
