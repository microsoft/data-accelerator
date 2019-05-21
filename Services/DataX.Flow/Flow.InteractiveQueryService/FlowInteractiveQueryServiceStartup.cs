// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
namespace Flow.InteractiveQueryService
{
    using Microsoft.AspNetCore.Hosting;
    using DataX.ServiceHost.AspNetCore;
    using DataX.ServiceHost.AspNetCore.Startup;
    using DataX.Contract.Settings;

    /// <summary>
    /// StartupFilter for Flow.InteractiveQueryService
    /// </summary>
    public class FlowInteractiveQueryServiceStartup : DataXServiceStartup
    {
        public FlowInteractiveQueryServiceStartup() { }

        public FlowInteractiveQueryServiceStartup(DataXSettings settings)
            : base(settings) { }
    }
}


