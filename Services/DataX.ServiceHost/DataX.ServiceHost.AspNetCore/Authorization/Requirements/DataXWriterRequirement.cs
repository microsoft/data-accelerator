namespace DataX.ServiceHost.AspNetCore.Authorization.Requirements
{
    using DataX.ServiceHost.Authorization.Requirements;
    using DataX.ServiceHost.Settings;
    using DataX.Utilities.Web;
    using Microsoft.AspNetCore.Authorization;

    internal class DataXWriterRequirement : DataXOneBoxRequirement
    {
        public DataXWriterRequirement() { }

        public DataXWriterRequirement(DataXSettings settings)
            : base(settings) { }

        protected override bool IsAuthorized(AuthorizationHandlerContext context, DataXSettings settings)
        {
            return base.IsAuthorized(context, settings) || context.User.IsInRole(RolesCheck.WriterRoleName);
        }
    }
}
