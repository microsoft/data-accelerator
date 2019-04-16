// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
namespace DataX.Utilities.Telemetry
{
    using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// OperationParentIdTelemetryInitializer is Creating a TelemetryInitializer to override the Operation_ParentId. This will used and called into by all the services
    /// </summary>
    public class OperationParentIdTelemetryInitializer : TelemetryInitializerBase
    {
        private const string _Value = "DataX-Service";
        public OperationParentIdTelemetryInitializer(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
        }
        /// <summary>
        /// overriding Operation_ParentId
        /// </summary>
        /// <param name="platformContext">platformContext</param>
        /// <param name="requestTelemetry">requestTelemetry</param>
        /// <param name="telemetry">telemetry</param>
        protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
        {
            if (!string.IsNullOrEmpty(telemetry.Context.Operation.ParentId))
            {
                telemetry.Context.Operation.ParentId = _Value;
            }
        }
    }
}
