namespace DataX.ServiceHost.ServiceFabric.Authorization
{
    using DataX.ServiceHost.Authorization.Requirements;
    using DataX.ServiceHost.Settings;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Authorization.Infrastructure;
    using System.Linq;
    public class DataXDefaultGatewayPolicy : DataXOneBoxRequirement
    {
        private DataXDefaultGatewayPolicy() { }

        public static void ConfigurePolicy(AuthorizationPolicyBuilder builder)
        {
            // ServiceFabric setup is the only setup with the Gateway
            if(HostUtil.InServiceFabric)
            {
                // Allow anonymous calls; RoleCheck will handle the checking in the controller
                builder.Requirements = builder.Requirements.Where(r => !(r is DenyAnonymousAuthorizationRequirement)).ToList();

                // Ensure the OneBox check
                builder.AddRequirements(new DataXDefaultGatewayPolicy());
            }
        }

        protected override bool IsAuthorized(AuthorizationHandlerContext context, DataXSettings settings)
        {
            return base.IsAuthorized(context, settings);
        }
    }
}
