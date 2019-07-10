// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Contract.Settings;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.ServiceHost.Authorization.Requirements
{
    public class DataXOneBoxRequirement : DataXAuthRequirement
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
