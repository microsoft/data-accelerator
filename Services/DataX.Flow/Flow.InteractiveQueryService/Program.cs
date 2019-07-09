// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.ServiceHost;
using DataX.ServiceHost.AspNetCore.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Flow.InteractiveQueryService
{
    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                if (HostUtil.InServiceFabric)
                {
                    // The ServiceManifest.XML file defines one or more service type names.
                    // Registering a service maps a service type name to a .NET type.
                    // When Service Fabric creates an instance of this service type,
                    // an instance of the class is created in this host process.

                    ServiceRuntime.RegisterServiceAsync("Flow.InteractiveQueryServiceType",
                        context => new InteractiveQueryService(context, WebHostBuilder)).GetAwaiter().GetResult();

                    ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(InteractiveQueryService).Name);

                    // Prevents this host process from terminating so services keeps running. 
                    Thread.Sleep(Timeout.Infinite);
                }
                else
                {
                    var webHost = WebHostBuilder.Build();

                    webHost.Start();
                    webHost.WaitForShutdown();
                }
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
                throw;
            }
        }

        /// <summary>
        /// Create a new WebHostBuilder with DataX default configuration that allows this to be run standalone
        /// </summary>
        private static IWebHostBuilder WebHostBuilder
            => new WebHostBuilder()
                    .UseDataXDefaultConfiguration<FlowInteractiveQueryServiceStartup>()
                    .Configure(app => app.UseDataXApplicationDefaults());
    }
}
