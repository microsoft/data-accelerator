using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.ServiceHost.AspNetCore.Authorization
{
    public class LocalCheckRequirement : IAuthorizationRequirement
    {
        public bool IsLocal { get; }

        public LocalCheckRequirement(bool isLocal)
        {
            this.IsLocal = isLocal;
        }
    }
}
