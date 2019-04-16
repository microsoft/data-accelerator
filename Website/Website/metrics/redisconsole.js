// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
const config = require('../config');
const redisServer = config.env.redisData.server;
require('redis-console')(redisServer);
