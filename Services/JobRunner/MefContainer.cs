// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

using DataX.Utilities.Composition;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.IO;
using System.Reflection;

namespace JobRunner
{
    public static class MefContainer
    {
        public static ContainerConfiguration GenerateConfiguration()
        {
            // Instead of building the CompositionContainer using the ConventionBuilder, we enumerate all the assemblies and create the container with them.
            // This will ensure that native dlls are not loaded into container creation which throws exception.
            List<Assembly> allAssemblies = new List<Assembly>();
            var executingDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            foreach (var file in Directory.EnumerateFiles(executingDir, "*.dll", SearchOption.AllDirectories))
            {
                try
                {
                    var assembly = Assembly.LoadFrom(file);
                    allAssemblies.Add(assembly);
                }
                catch (BadImageFormatException)
                {
                    // do nothing and skip the assembly to load as it might be the native assemblies
                }
            }

            var mefConfig = new ContainerConfiguration().WithAssemblies(allAssemblies);
            return mefConfig;
        }

        public static CompositionHost CreateWithConfiguration(IConfiguration configuration)
        {
            var mefConfig = GenerateConfiguration().WithProvider(new InstanceExportDescriptorProvider<IConfiguration>(configuration));
            return mefConfig.CreateContainer();
        }

        public static IConfiguration BuildConfigurationForConsoleApp(string environment = null)
        {
            if (string.IsNullOrWhiteSpace(environment))
            {
                // Load the ability to specify env. in deployed webjob.
                environment = Environment.GetEnvironmentVariable("ENVIRONMENT");
            }

            // Load appsettings.json, and any env specific appsettings in the form "apsettings.<env>.json" matching the ENVIRONMENT variable.
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables("DATAX_")
                .Build();

            return config;
        }
    }
}
