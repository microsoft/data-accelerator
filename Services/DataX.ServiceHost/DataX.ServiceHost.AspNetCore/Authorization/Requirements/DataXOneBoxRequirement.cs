using DataX.ServiceHost.Settings;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.ServiceHost.AspNetCore.Authorization.Requirements
{
    internal class DataXOneBoxRequirement : DataXAuthRequirement
    {
        public DataXOneBoxRequirement() { }

        public DataXOneBoxRequirement(DataXSettings settings)
            : base(settings) { }

        protected override bool IsAuthorized(AuthorizationHandlerContext context, DataXSettings settings)
        {
            return settings.EnableOneBox;
        }
    }
}
