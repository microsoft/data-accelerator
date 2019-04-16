// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
const express = require('express');
const http = require('http');
const app = express();
const server = http.Server(app);

const host = {
    app: app,
    server: server
};

module.exports = host;
