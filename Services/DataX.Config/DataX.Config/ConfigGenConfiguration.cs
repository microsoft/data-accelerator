// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Contract.Exception;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;

namespace DataX.Config
{
    /// <summary>
    /// Main Configuration class that holds all the configuration settings coming from various providers.
    /// </summary>
    [Shared]
    [Export]
    public class ConfigGenConfiguration
    {
        [ImportingConstructor]
        public ConfigGenConfiguration([ImportMany]IEnumerable<IConfigGenConfigurationProvider> providers)
        {
            ConfigurationProviders = (providers??Enumerable.Empty<IConfigGenConfigurationProvider>()).OrderBy(p => p.GetOrder()).ToArray();
        }

        private IConfigGenConfigurationProvider[] ConfigurationProviders { get; }

        public string this[string key]
        {
            get
            {
                if(TryGet(key, out var value))
                {
                    return value;
                }
                else
                {
                    throw new GeneralException($"Cannot find configuration setting '{key}'");
                }
            }
        }

        public bool TryGet(string key, out string value)
        {
            foreach (var provider in ConfigurationProviders)
            {
                if (provider.TryGet(key, out var v))
                {
                    value = v;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public IDictionary<string, string> GetAllSettings()
        {
            var dict = new Dictionary<string, string>();

            foreach(var provider in ConfigurationProviders)
            {
                dict.AppendDictionary(provider.GetAllSettings());
            }

            return dict;
        }

        public string GetOrDefault(string key, string defaultValue)
        {
            if (TryGet(key, out var value))
            {
                return value;
            }
            else
            {
                return defaultValue;
            }
        }
    }
}
