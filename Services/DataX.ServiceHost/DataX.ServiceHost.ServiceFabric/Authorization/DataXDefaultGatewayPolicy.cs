// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
namespace DataX.ServiceHost.ServiceFabric.Authorization
{
    using DataX.ServiceHost.Authorization;
    using DataX.Contract.Settings;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Authorization.Infrastructure;
    using System.Linq;

    /// <inheritdoc />
    public class DataXDefaultGatewayPolicy : DataXAuthRequirement
    {
        private DataXDefaultGatewayPolicy() { }

        /// <summary>
        /// Configures the default, recommended policy for running in ServiceFabric behind Gateway
        /// </summary>
        /// <param name="builder">The AuthorizationPolicyBuilder to modify</param>
        public static void ConfigurePolicy(AuthorizationPolicyBuilder builder)
        {
            // ServiceFabric setup is the only setup with the Gateway
            if(HostUtil.InServiceFabric)
            {
                // Allow anonymous calls; RoleCheck will handle the checking in the controller
                builder.Requirements = builder.Requirements.Where(r => !(r is DenyAnonymousAuthorizationRequirement || r is DataXAuthRequirement)).ToList();

                // Ensure the OneBox check
                builder.AddRequirements(new DataXDefaultGatewayPolicy());
            }
        }

        /// <inheritdoc />
        protected override bool IsAuthorized(AuthorizationHandlerContext context, DataXSettings settings)
        {
            // If OneBox, no auth. If in SF, no auth (handled in Gateway). True in both scenarios, so returning true.
            return true;
        }
    }
}
