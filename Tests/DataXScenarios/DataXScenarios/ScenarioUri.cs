// *********************************************************************
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License
// *********************************************************************

namespace DataXScenarios
{
    public class ScenarioUri
    {
        public const string SaveFlow = "/api/DataX.Flow/Flow.ManagementService/flow/save";
        public const string StartFlowJobs = "/api/DataX.Flow/Flow.ManagementService/flow/startjobs";
        public const string GenerateConfigs = "/api/DataX.Flow/Flow.ManagementService/flow/generateconfigs";
        public const string RestartFlowJobs = "/api/DataX.Flow/Flow.ManagementService/flow/restartjobs";
        public const string GetFlow = "/api/DataX.Flow/Flow.ManagementService/flow/get";
        public const string GetFlowParamName = "flowName";
        public const string InferSchema = "/api/DataX.Flow/Flow.SchemaInferenceService/inputdata/inferschema";
        public const string initializeKernel = "/api/DataX.Flow/Flow.InteractiveQueryService/kernel";
        public const string RefreshKernel = "/api/DataX.Flow/Flow.InteractiveQueryService/kernel/refresh";
        public const string RefreshSample = "/api/DataX.Flow/Flow.SchemaInferenceService/inputdata/refreshsample";
        public const string RefreshSampleAndKernel = "/api/DataX.Flow/Flow.LiveDataService/inputdata/refreshsampleandkernel";
        public const string DeleteKernel = "/api/DataX.Flow/Flow.InteractiveQueryService/kernel/delete";
    }
}
