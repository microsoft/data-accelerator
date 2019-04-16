// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System.Collections.Generic;

namespace DataX.Config
{
    /// <summary>
    /// Interface for providing configuration settings into the services pool
    /// Note the provider implementation can only import InitialConfiguration when needed,
    /// other imports could fail because they are waiting for all configuration providers to be instantiated first.
    /// </summary>
    public interface IConfigGenConfigurationProvider
    {
        /// <summary>
        /// Tries to get a configuration value for the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>True</c> if a value for the specified key was found, otherwise <c>false</c>.</returns>
        bool TryGet(string key, out string value);

        /// <summary>
        /// Gets the order of this provider, set to 1000 for general settings provider,
        /// set lower value if the provider want to override other providers
        /// </summary>
        /// <returns></returns>
        int GetOrder();

        /// <summary>
        /// Gets all settings and values from the provider
        /// </summary>
        /// <returns></returns>
        IDictionary<string, string> GetAllSettings();
    }
}
