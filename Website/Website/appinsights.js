// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
const appInsights = require('applicationinsights');

module.exports = host => {
    let env = host.conf.env;
    let aiKey = env.aiKey;
    if (aiKey) {
        appInsights
            .setup(aiKey)
            .setAutoDependencyCorrelation(true)
            .setAutoCollectRequests(true)
            .setAutoCollectPerformance(true)
            .setAutoCollectExceptions(true)
            .setAutoCollectDependencies(true)
            .setAutoCollectConsole(true)
            .setUseDiskRetryCaching(true)
            .start();

        appInsights.defaultClient.commonProperties = {
            env: env.name
        };
    } else {
        //appInsights.defaultClient.config.disableAppInsights = true;
        appInsights.defaultClient = {
            trackEvent: () => {},
            trackNodeHttpRequest: () => {}
        };
    }

    return (host.telemetryClient = appInsights.defaultClient);
};
