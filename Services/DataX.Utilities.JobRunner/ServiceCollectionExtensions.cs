
// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Composition.Hosting;

namespace JobRunner
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add all MEF dependencies to service collection
        /// </summary>
        /// <param name="services">ASP.NET core service collection container</param>
        /// <param name="exportTypes">Dictionary of Type to be exported with its specified lifetime.</param>
        public static CompositionHost AddMefExportsFromAssemblies(this IServiceCollection services, Dictionary<Type, ServiceLifetime> exportTypes, IConfiguration configuration)
        {
            var container = MefContainer.CreateWithConfiguration(configuration);
            foreach (var exportType in exportTypes)
            {
                var svcs = container.GetExports(exportType.Key);
                foreach (var svc in svcs)
                {
                    services.Add(new ServiceDescriptor(exportType.Key, serviceProvider => svc, exportType.Value));
                }
            }

            return container;
        }
    }
}
