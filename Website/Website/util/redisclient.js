// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
var log = require('../logger');
var redis = require('redis');

function parseRedisConnectionString(conn) {
    try {
        var r = conn.match(/^([^:]*):(\d+),password=([^,]*),/);
        return {
            host: r[1],
            port: r[2],
            accessKey: r[3]
        };
    } catch (error) {
        //Avoid throwing when config is not fully set up on dev machine
        log.error('Environment for redis connection not set properly; see readme.md to set up environment');
    }
    return {
        host: '',
        port: '',
        accessKey: ''
    };
}

module.exports = function(redisConnectionString) {
    let redisServer = parseRedisConnectionString(redisConnectionString);

    log.info('connecting to redis server:"' + redisServer.host + '"');
    return redis.createClient(redisServer.port, redisServer.host, {
        auth_pass: redisServer.accessKey,
        tls: {
            servername: redisServer.host
        },
        retry_strategy: function(options) {
            if (options.error) {
                if (options.error.code === 'ECONNREFUSED') {
                    var msg = 'The redis server ' + redisServer.host + ':' + redisServer.port + ' refused the connection.';
                    log.error(msg);
                    return new Error(msg);
                } else {
                    log.error(options.error);
                }
            }

            return Math.min(options.attempt * 100, 3000);
        }
    });
};
