namespace DataX.ServiceHost.AspNetCore.Authorization.Requirements
{
    using DataX.ServiceHost.Authorization.Requirements;
    using DataX.Contract.Settings;
    using DataX.Utilities.Web;
    using Microsoft.AspNetCore.Authorization;

    /// <inheritdoc />
    internal class DataXWriterRequirement : DataXOneBoxRequirement
    {
        public DataXWriterRequirement() { }

        public DataXWriterRequirement(DataXSettings settings)
            : base(settings) { }

        /// <inheritdoc />
        protected override bool IsAuthorized(AuthorizationHandlerContext context, DataXSettings settings)
        {
            return base.IsAuthorized(context, settings) || context.User.IsInRole(RolesCheck.WriterRoleName);
        }
    }
}
