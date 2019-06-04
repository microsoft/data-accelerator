// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
namespace DataX.ServiceHost.AspNetCore.Authorization.Requirements
{
    using DataX.Contract.Settings;
    using DataX.Utilities.Web;
    using Microsoft.AspNetCore.Authorization;

    /// <inheritdoc />
    internal class DataXReaderRequirement : DataXWriterRequirement
    {
        public DataXReaderRequirement() { }

        public DataXReaderRequirement(DataXSettings settings)
            : base(settings) { }

        /// <inheritdoc />
        protected override bool IsAuthorized(AuthorizationHandlerContext context, DataXSettings settings)
        {
            return base.IsAuthorized(context, settings) || context.User.IsInRole(RolesCheck.ReaderRoleName);
        }
    }
}
