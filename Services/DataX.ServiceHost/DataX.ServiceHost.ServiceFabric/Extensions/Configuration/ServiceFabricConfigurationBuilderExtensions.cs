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
        /// <summary>
        /// Adds the specified service fabric settings to the configuration builder. If service fabric is not detected, nothing is added.
        /// </summary>
        /// <param name="packageName">The configuration package object name</param>
        /// <param name="configPrefix">An optional prefix to add to the configurations added to the builder. e.g. "MySettings:"</param>
        public static IConfigurationBuilder AddServiceFabricSettings(this IConfigurationBuilder configurationBuilder, string packageName, string configPrefix = "")
        {
            if (configurationBuilder == null)
            {
                throw new ArgumentNullException(nameof(configurationBuilder));
            }

            if (HostUtil.InServiceFabric)
            {
                configurationBuilder.Add(new ServiceFabricConfigurationSource(packageName, configPrefix));
            }

            return configurationBuilder;
        }
    }
}
