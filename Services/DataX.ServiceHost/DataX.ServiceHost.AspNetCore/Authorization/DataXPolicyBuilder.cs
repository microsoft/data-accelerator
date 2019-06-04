// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
namespace DataX.ServiceHost.AspNetCore.Authorization
{
    using DataX.ServiceHost.Authorization;
    using DataX.Contract.Settings;
    using Microsoft.AspNetCore.Authorization;
    using System;
    using System.Linq;

    /// <summary>
    /// This class is meant to simplify the syntax for adding requirements for policies
    /// </summary>
    internal class DataXPolicyBuilder
    {
        private readonly AuthorizationOptions _options;
        private readonly DataXSettings _settings;
        private readonly Action<AuthorizationPolicyBuilder> _configurePolicy;

        public DataXPolicyBuilder(
            AuthorizationOptions options,
            DataXSettings settings,
            Action<AuthorizationPolicyBuilder> configurePolicy)
        {
            _options = options;
            _settings = settings;
            _configurePolicy = configurePolicy;
        }

        public DataXPolicyBuilder AddPolicy<TRequirement>(string name)
            where TRequirement : DataXAuthRequirement, new()
        {
            _options.AddPolicy(name, DataXPolicy<TRequirement>);

            return this;
        }

        private void DataXPolicy<TDataXRequirement>(AuthorizationPolicyBuilder policy)
            where TDataXRequirement : DataXAuthRequirement, new()
        {
            AddDataXRequirements<TDataXRequirement>(policy);
        }

        /// <summary>
        /// Adds the basic DataX auth policy to the builder
        /// </summary>
        private AuthorizationPolicyBuilder AddDataXRequirements<TDataXRequirement>(AuthorizationPolicyBuilder policy)
            where TDataXRequirement : DataXAuthRequirement, new()
        {
            // We don't want to add the same requirement in again.
            // If it does exist and the settings changed, then we want to make sure the new settings are used
            RemoveDataXRequirements(policy);

            var requirement = new TDataXRequirement()
            {
                Settings = _settings
            };

            if (!_settings.EnableOneBox)
            {
                policy.RequireAuthenticatedUser();
            }

            policy.AddRequirements(requirement);
            _configurePolicy?.Invoke(policy);

            return policy;
        }

        /// <summary>
        /// Removes the DataXRequirements set in the policy builder if they exist.
        /// </summary>
        private static AuthorizationPolicyBuilder RemoveDataXRequirements(AuthorizationPolicyBuilder policy)
        {
            var requirements = policy.Requirements.Where(req => req is DataXAuthRequirement);

            foreach (var req in requirements)
            {
                policy.Requirements.Remove(req);
            }

            return policy;
        }
    }
}
