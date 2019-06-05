// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
namespace Flow.SchemaInferenceService
{
    using Microsoft.AspNetCore.Hosting;
    using DataX.ServiceHost.AspNetCore;
    using DataX.ServiceHost.AspNetCore.Startup;
    using DataX.Contract.Settings;

    /// <summary>
    /// StartupFilter for Flow.SchemaInferenceService
    /// </summary>
    public class FlowSchemaInferenceServiceStartup : DataXServiceStartup
    {
        public FlowSchemaInferenceServiceStartup() { }

        public FlowSchemaInferenceServiceStartup(DataXSettings settings)
            : base(settings) { }
    }
}


