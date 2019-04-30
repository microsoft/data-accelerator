// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

namespace DataX.Config.ServiceFabric.Extensions.Configuration
{
    using Microsoft.Extensions.Configuration;
    using System;

    public static class ServiceFabricConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddInMemoryCollection(this IConfigurationBuilder configurationBuilder, string packageName)
        {
            if (configurationBuilder == null)
            {
                throw new ArgumentNullException("configurationBuilder");
            }
            configurationBuilder.Add(new ServiceFabricConfigurationSource(packageName));
            return configurationBuilder;
        }
    }
}
