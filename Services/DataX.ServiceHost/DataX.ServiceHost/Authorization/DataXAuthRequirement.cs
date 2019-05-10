namespace DataX.ServiceHost.Authorization
{
    using DataX.ServiceHost.Settings;
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
