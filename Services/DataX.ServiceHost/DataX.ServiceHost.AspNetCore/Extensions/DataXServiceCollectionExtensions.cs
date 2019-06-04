// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.ServiceHost.AspNetCore.Startup;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace DataX.ServiceHost.AspNetCore.Extensions
{
    /// <summary>
    /// Extensions for IServiceCollection
    /// </summary>
    public static class DataXServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the specified DataX startup filter to the provides services.
        /// </summary>
        /// <typeparam name="TStartup">A type deriving from <see cref="DataXServiceStartup"/></typeparam>
        /// <param name="services">The services to add the DataX startup to</param>
        /// <param name="startup">Optional startup instance</param>
        /// <returns>The configured services</returns>
        public static IServiceCollection AddDataXStartup<TStartup>(this IServiceCollection services, TStartup startup = null)
            where TStartup : DataXServiceStartup, new()
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            startup = startup ?? new TStartup();

            startup.ConfigureServices(services);

            services.AddTransient<IStartupFilter>(_ => startup);

            return services;
        }

        /// <summary>
        /// Removes DataX controllers based on a provided DataX startup type
        /// </summary>
        /// <typeparam name="TStartup">Concrete startup type for assembly reference</typeparam>
        /// <param name="manager">The application part manager to remove DataX controllers from</param>
        /// <returns>The modified application part manager</returns>
        public static ApplicationPartManager RemoveDataXController<TStartup>(this ApplicationPartManager manager)
            where TStartup : DataXServiceStartup, new()
        {
            var startupAssembly = typeof(TStartup).Assembly;

            var partsToRemove = manager.ApplicationParts.OfType<AssemblyPart>().Where(p => p.Assembly == startupAssembly);

            foreach (var part in partsToRemove)
            {
                manager.ApplicationParts.Remove(part);
            }

            return manager;
        }
    }
}
