// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
const express = require('express');
const config = require('../config');
const telemetryClient = require('applicationinsights').defaultClient;
const env = config.env;
const defaultDistName = 'dev';

var distName = null;
if (process.argv.length > 2) {
    var option = process.argv[2];
    if (option.startsWith('--dist=')) {
        distName = option.split('=')[1];
    }
}

const distPrefix = 'dist/';
distName = distName || env.dist || defaultDist;

var dist = distPrefix + distName;

function sendStaticFile(res, builtPath) {
    res.sendFile(builtPath, { root: __dirname + '/../' + dist });
}

module.exports = {
    dist: dist,
    name: distName,
    sendStaticFile: sendStaticFile,
    initialize: function(app) {
        app.use(express.static(dist));
        app.use(express.static(distPrefix + 'shared'));
    },
    withEnv: function(env) {
        if (env && env.dist) {
            distName = env.dist;
            dist = distPrefix + distName;
        }

        return {
            dist: dist,
            name: distName
        };
    }
};
