// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
namespace DataX.ServiceHost.Authorization
{
    using DataX.Contract.Settings;
    using Microsoft.AspNetCore.Authorization;
    using System.Threading.Tasks;

    /// <summary>
    /// For assertion requirements. We're extending this so that we can easily identify the DataX requirement instance
    /// when adding in policy requirements. This lets us prevent duplication of requirements and handlers.
    /// </summary>
    public abstract class DataXAuthRequirement : IAuthorizationHandler, IAuthorizationRequirement
    {
        public DataXSettings Settings { get; set; }

        public DataXAuthRequirement() { }

        public DataXAuthRequirement(DataXSettings settings)
        {
            Settings = settings;
        }

        /// <inheritdoc />
        public Task HandleAsync(AuthorizationHandlerContext context)
        {
            if(IsAuthorized(context, Settings))
            {
                context.Succeed(this);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Given the auth context and settings, determines if a request is authorized.
        /// </summary>
        /// <param name="context">Provided AuthorizationHandlerContext</param>
        /// <param name="settings">Provided DataXSettings</param>
        /// <returns>True if determined to be authorized, else false.</returns>
        protected abstract bool IsAuthorized(AuthorizationHandlerContext context, DataXSettings settings);
    }
}
