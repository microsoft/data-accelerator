// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System.Fabric;

namespace DataX.Metrics.Ingestor.Helper
{
    /// <summary>
    /// Utility class
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// Gets a parameter value
        /// </summary>
        /// <param name="name">parameter name</param>
        /// <returns>parameter value</returns>
        public static string GetConfigValue(string name)
        {
            ConfigurationPackage package = FabricRuntime.GetActivationContext().GetConfigurationPackageObject("Config");
            System.Fabric.Description.ConfigurationSection serviceEnvironmentSection = package.Settings.Sections["ServiceEnvironment"];
            var value = serviceEnvironmentSection.Parameters[name].Value;
            return value;
        }

    }
}
