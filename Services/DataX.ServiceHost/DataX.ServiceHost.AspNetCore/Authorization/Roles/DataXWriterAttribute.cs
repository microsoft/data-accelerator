// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.ServiceHost.AspNetCore.Authorization.Roles
{
    /// <inheritdoc />
    public class DataXWriterAttribute : DataXAuthorizeAttribute
    {
        public DataXWriterAttribute()
        {
            Policy = DataXAuthConstants.WriterPolicyName;
        }
    }
}
