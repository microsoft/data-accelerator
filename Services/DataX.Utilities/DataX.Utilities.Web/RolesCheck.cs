// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.AspNetCore.Http;
using DataX.Contract;
using System;
using DataX.Gateway.Contract;
using DataX.ServiceHost;

namespace DataX.Utilities.Web
{
    public static class RolesCheck
    {
        // Default role names for reader/writer. These are made public so that service can override these values.
        public static string ReaderRoleName { get; set; } = "DataXReader";
        public static string WriterRoleName { get; set; } = "DataXWriter";

        public static void EnsureWriter(HttpRequest request, bool isLocal)
        {
            // If the request is for localhost, ignore roles check. This is to enable local onebox scenario
            if (!isLocal)
            {
                EnsureWriter(request);
            }
        }

        public static void EnsureWriter(HttpRequest request)
        {
            // Ensure* methods only work when auth is handled at the Gateway in Service Fabric setup
            // Otherwise ASP.NET Core is used and does not require this check
            if(!HostUtil.InServiceFabric)
            {
                return;
            }

            var userrole = request.Headers[Constants.UserRolesHeader];
            Ensure.NotNull(userrole, "userrole");

            if (!userrole.ToString().Contains(WriterRoleName))
            {
                throw new System.Exception($"{WriterRoleName} role needed to perform this action.  User has the following roles: {userrole}");
            }
        }

        public static void EnsureReader(HttpRequest request, bool isLocal)
        {
            // If the request is for localhost, ignore roles check. This is to enable local onebox scenario
            if (!isLocal)
            {
                EnsureReader(request);
            }
        }

        public static void EnsureReader(HttpRequest request)
        {
            // Ensure* methods only work when auth is handled at the Gateway in Service Fabric setup
            // Otherwise ASP.NET Core is used and does not require this check
            if (!HostUtil.InServiceFabric)
            {
                return;
            }

            var userrole = request.Headers[Constants.UserRolesHeader];
            Ensure.NotNull(userrole, "userrole");

            if (!userrole.ToString().Contains(ReaderRoleName) && !userrole.ToString().Contains(WriterRoleName))
            {
                throw new System.Exception($"{ReaderRoleName} role needed to perform this action.  User has the following roles: {userrole}");
            }
        }
    }
}

