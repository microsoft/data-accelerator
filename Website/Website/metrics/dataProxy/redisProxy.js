// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
var log = require('../../logger');
var redis = require('../../util/redisclient');
var Q = require('q');

function schemaHandler(schema, redisClient, clientIo, getRoomForSchema) {
    var queueName = schema.queueName;
    var pubName = schema.pubName;
    var roomName = getRoomForSchema(schema.name);
    var roomIo = clientIo.to(roomName);
    var previousTime = Date.now();

    function handleSubscribed(data) {
        roomIo.emit('datapoints', schema.name, data);
        log.info('sent ' + data.length + ' datapoints of ' + schema.name);
    }

    function pollData(currentTime) {
        var defer = Q.defer();
        redisClient.zrangebyscore(queueName, '(' + previousTime, currentTime, function(err, result) {
            if (err) {
                log.error(err);
                defer.reject(err);
            } else {
                if (result.length > 0) {
                    roomIo.emit('datapoints', schema, result.map(JSON.parse));
                    previousTime = currentTime;
                    log.info('sent ' + result.length + ' datapoints of ' + schema.name);
                }
                defer.resolve(result.length);
            }
        });

        return defer.promise;
    }

    function fakeData(n) {
        n = n || 1;
        var data = [];
        for (var i = 0; i < n; i++) data.push({ value: Math.random() * 60, ts: new Date().getMilliseconds() });

        roomIo.emit('datapoints', schema, data);
        log.info('sent ' + data.length + ' fake datapoints of ' + schema.name);
    }

    function polling() {
        var currentTime = Date.now();
        pollData(currentTime).then(() => {
            setTimeout(polling, 700);
        });
    }

    function faking() {
        fakeData();
        setTimeout(faking, 1400);
    }

    switch (schema.generation) {
        case 'fake':
            faking();
            break;
    }

    return {
        config: function(clientId) {
            return {};
        },
        initData: function(socket, clientId, startTime) {
            socket.emit('init-data', clientId, schema.name, {});

            if (queueName) {
                redisClient.zrangebyscore(queueName, '(' + startTime, previousTime, function(err, result) {
                    if (err) log.error(err);
                    else {
                        if (result.length > 0) {
                            socket.emit('datapoints', schema.name, result.map(JSON.parse));
                            log.info('sent ' + result.length + ' datapoints of ' + schema.name);
                        }
                    }
                });
            }
        },
        subscribe: function(channels, redisSub) {
            if (pubName) {
                redisSub.subscribe(pubName);
                channels[pubName] = handleSubscribed;
                return handleSubscribed;
            }
        }
    };
}

function service(redisData, clientIo, getRoomForSchema) {
    var redisClient = redis(redisData.server);
    var schemas = {};
    redisData.schemas.forEach(function(schema) {
        schemas[schema.name] = schemaHandler(schema, redisClient, clientIo, getRoomForSchema);
    });

    var channels = {},
        redisSub = redis(redisData.server);
    redisSub.on('message', function(channel, message) {
        var data = [];
        try {
            data = JSON.parse(message);
        } catch (error) {
            log.error(error);
        }

        if (channel in channels) {
            channels[channel](data);
        } else {
            log.error('Could not find handler for channel:"' + channel + '"');
        }
    });
    redisSub.on('subscribe', function(channel, count) {
        log.info("Subscribed to '" + channel + "':" + count);
    });

    redisData.schemas.forEach(function(schema) {
        schemas[schema.name].subscribe(channels, redisSub);
    });

    return {
        canHandle: function(schema) {
            return schema in schemas;
        },
        initDataConfig: function(socket, clientId, schema) {
            var schemaHandler = schemas[schema];
            socket.emit('config', clientId, schema, schemaHandler.config(clientId));
        },
        initData: function(socket, clientId, schema, timestamp) {
            var schemaHandler = schemas[schema];
            var startTime = new Date(timestamp).getTime();
            var roomName = getRoomForSchema(schema);
            socket.join(roomName);

            schemaHandler.initData(socket, clientId, startTime);
        }
    };
}

module.exports = service;
