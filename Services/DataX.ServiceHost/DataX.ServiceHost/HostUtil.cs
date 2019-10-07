// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.ServiceHost
{
    public static class HostUtil
    {
        /// <summary>
        /// Returns true if Fabric_ApplicationName environment variable is not null, otherwise false if it is null.
        /// </summary>
        public static bool InServiceFabric => Environment.GetEnvironmentVariable("Fabric_ApplicationName") != null;
    }
}
