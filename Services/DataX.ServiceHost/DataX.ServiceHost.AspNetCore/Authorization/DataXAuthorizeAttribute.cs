// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Utilities.Web;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataX.ServiceHost.AspNetCore.Authorization
{
    /// <inheritdoc />
    public abstract class DataXAuthorizeAttribute : AuthorizeAttribute
    {
        public DataXAuthorizeAttribute()
        {
            Policy = DataXAuthConstants.PolicyPrefix;
        }
    }
}
