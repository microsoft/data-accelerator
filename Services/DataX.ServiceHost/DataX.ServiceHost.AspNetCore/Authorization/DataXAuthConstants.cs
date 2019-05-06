using DataX.Utilities.Web;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.ServiceHost.AspNetCore.Authorization
{
    public static class DataXAuthConstants
    {
        public const string PolicyPrefix = "DataXAuth_";

        public static string WriterPolicyName { get; } = PolicyPrefix + RolesCheck.WriterRoleName;

        public static string ReaderPolicyName { get; } = PolicyPrefix + RolesCheck.ReaderRoleName;
    }
}
