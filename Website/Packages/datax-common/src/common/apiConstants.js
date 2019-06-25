// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
export const Constants = {
    // Application name
    serviceApplication: 'DataX.Flow',

    // Service application route api
    serviceRouteApi: '/api/apiservice',

    // List of supported services
    services: {
        interactiveQuery: 'Flow.InteractiveQueryService',
        schemaInference: 'Flow.SchemaInferenceService',
        liveData: 'Flow.LiveDataService',
        flow: 'Flow.ManagementService'
    }
};

export const ApiNames = {
    FunctionEnabled: 'functionenabled',
    IsDatabricksSparkType: 'isdatabrickssparktype',
    // OneBox
    EnableLocalOneBox: 'enableLocalOneBox'
};
