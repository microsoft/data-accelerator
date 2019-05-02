// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

namespace DataX.ServiceHost.ServiceFabric
{
    using Microsoft.Extensions.Configuration.Memory;
    using System.Collections.Generic;
    using System.Fabric;

    public class ServiceFabricConfigurationSource : MemoryConfigurationSource
    {
        private readonly string _configPrefix;

        public ServiceFabricConfigurationSource(string packageName, string configPrefix)
        {
            _configPrefix = configPrefix.EndsWith(":") ? configPrefix : configPrefix + ":";

            var package = FabricRuntime.GetActivationContext().GetConfigurationPackageObject(packageName);

            this.InitialData = GetFlatConfig(package);
        }

        private IEnumerable<KeyValuePair<string,string>> GetFlatConfig(ConfigurationPackage package)
        {
            var flatConfig = new Dictionary<string, string>();
            foreach (var section in package.Settings.Sections)
            {
                foreach (var parameter in section.Parameters)
                {
                    flatConfig[$"{_configPrefix}{section.Name}:{parameter.Name}"] = parameter.Value;
                }
            }

            return flatConfig;
        }
    }
}
