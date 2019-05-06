using DataX.ServiceHost.Settings;
using DataX.Utilities.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataX.ServiceHost.AspNetCore.Authorization
{
    /// <summary>
    /// A for assertion requirements. We're extending this so that we can easily identify the DataX requirement instance
    /// when adding in policy requirements. This lets us prevent duplication of requirements and handlers.
    /// This is made internal as using it outside of this context may cause configuration issues if used improperly.
    /// </summary>
    internal abstract class DataXAuthRequirement : IAuthorizationHandler, IAuthorizationRequirement
    {
        public DataXSettings Settings { get; set; }

        public DataXAuthRequirement() { }

        public DataXAuthRequirement(DataXSettings settings)
        {
            Settings = settings;
        }

        public Task HandleAsync(AuthorizationHandlerContext context)
        {
            if(IsAuthorized(context, Settings))
            {
                context.Succeed(this);
            }

            return Task.CompletedTask;
        }

        protected abstract bool IsAuthorized(AuthorizationHandlerContext context, DataXSettings settings);
    }
}
