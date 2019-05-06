using DataX.Utilities.Web;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataX.ServiceHost.AspNetCore.Authorization
{
    public class DataXRoleRequirementAttribute : AuthorizeAttribute
    {
        private const string _PolicyPrefix = "MinimumAge";

        public bool LocalOverride { get; set; } = true;

        public DataXRoleRequirementAttribute()
        {
            Roles = $"{nameof(RolesCheck.ReaderRoleName)},{nameof(RolesCheck.WriterRoleName)}";
            Policy = nameof()
        }
    }
}
