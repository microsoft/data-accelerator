// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.Extensions.DependencyInjection;
using DataX.Config.Utility;
using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.Reflection;
using DataX.Contract;
using Microsoft.Extensions.Logging;

namespace Flow.Management
{
    // Extension method to add MEF dependencies to ASP.NET Core service collection
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add MEF dependencies to service collection
        /// </summary>
        /// <param name="services">ASP.NET core service collection container</param>
        /// <param name="lifetime">Lifetime enum value</param>
        /// <param name="assemblies">Assemblies to export the dependencies from</param>
        /// <param name="exportTypes">Types being exported</param>
        /// <param name="instanceExports">Instances that are created outside that need to be added to the container</param>
        /// <returns></returns>
        public static IServiceCollection AddMefExportsFromAssemblies(this IServiceCollection services, ServiceLifetime lifetime, IEnumerable<Assembly> assemblies, Type[] exportTypes, object[] instanceExports, ILoggerFactory loggerFactory = null, bool local = false)
        {          
            var configuration = new ContainerConfiguration().WithAssemblies(assemblies).WithProvider(new LoggerAndInstanceExportDescriptorProvider<object[]>(instanceExports, loggerFactory));
            using (var container = configuration.CreateContainer())
            {
                foreach (var exportType in exportTypes)
                {
                    var svcs = container.GetExports(exportType);
                    foreach (var svc in svcs)
                    {
                        services.Add(new ServiceDescriptor(exportType, sp => svc, lifetime));
                    }
                }

                if (local)
                {
                    var localTemplateInitializer = container.GetExport<DataX.Config.Local.TemplateInitializer>();
                    var result = localTemplateInitializer?.Initialize().Result;

                    Ensure.IsSuccessResult(result);
                }

            }
            return services;
        }
    }
}
