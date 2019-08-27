// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JobRunner
{
    /// <summary>
    /// JobRunnerWebJob is the entrypoint of the continuously running functionality of JobRunner.
    /// All logic, libs, etc, are in the core JobRunner project.  This is only a wrapper.
    /// </summary>
    internal class Program
    {
        public static void Main(string[] args)
        {
            var host = StartupHelper.HostWebWith<Startup>(args);
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            host.Run();
        }
    }
}
