// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
namespace DataX.Gateway.Contract
{
    /// <summary>
    /// Constants used by DataX.Gateway
    /// </summary>
    public static class Constants
    {
        public const string UserNameHeader = "X-DataX-UserName";
        public const string UserIdHeader = "X-DataX-UserId";
        public const string UserGroupsHeader = "X-DataX-UserGroups";
        public const string UserRolesHeader = "X-DataX-UserRoles";
        public const string ReverseProxyPort = "19081";
        public const int DefaultHttpTimeoutSecs = 240;
    }
}
