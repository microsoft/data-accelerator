// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
export const JobState = {
    Idle: 'Idle',
    Starting: 'Starting',
    Running: 'Running',
    Error: 'Error'
};

export const Constants = {
    // Application name
    serviceApplication: 'DataX.Flow',

    // Service application route api
    serviceRouteApi: '/api/apiservice',

    // List of supported services
    services: {
        flow: 'Flow.ManagementService'
    }
};