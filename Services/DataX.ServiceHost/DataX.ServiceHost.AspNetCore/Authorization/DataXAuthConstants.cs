// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Utilities.Web;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.ServiceHost.AspNetCore.Authorization
{
    /// <summary>
    /// Constants used in DataX auth scenarios
    /// </summary>
    public static class DataXAuthConstants
    {
        public const string PolicyPrefix = "DataXAuth_";

        public static string WriterPolicyName { get; } = PolicyPrefix + RolesCheck.WriterRoleName;

        public static string ReaderPolicyName { get; } = PolicyPrefix + RolesCheck.ReaderRoleName;
    }
}
