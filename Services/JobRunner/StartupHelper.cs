// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace JobRunner
{
    /// <summary>
    /// Helper that gets called from startup for the JobRunner service.
    /// </summary>
    public static class StartupHelper
    {
        public static IWebHost HostWebWith<TStartup>(string[] args) where TStartup : class
        {
            var host = CreateWebHostBuilder<TStartup>(args).Build();
            return host;            
        }

        public static IWebHostBuilder CreateWebHostBuilder<TStartup>(string[] args) where TStartup : class
        {
            return WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) => config.AddCommandLine(args).AddEnvironmentVariables(prefix: "DATAX_"))
                .UseStartup<TStartup>();
                
        }
        
    }
}
