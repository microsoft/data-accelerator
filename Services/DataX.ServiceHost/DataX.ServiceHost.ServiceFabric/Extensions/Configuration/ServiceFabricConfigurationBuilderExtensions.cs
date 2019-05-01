// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

namespace DataX.ServiceHost.ServiceFabric.Extensions.Configuration
{
    using Microsoft.Extensions.Configuration;
    using System;

    public static class ServiceFabricConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddServiceFabricSettings(this IConfigurationBuilder configurationBuilder, string packageName)
        {
            if (configurationBuilder == null)
            {
                throw new ArgumentNullException(nameof(configurationBuilder));
            }

            if (Environment.GetEnvironmentVariable("Fabric_ApplicationName") != null)
            {
                configurationBuilder.Add(new ServiceFabricConfigurationSource(packageName));
            }

            return configurationBuilder;
        }
    }
}
