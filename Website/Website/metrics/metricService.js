// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
const config = require('../config');
const log = require('../logger');
var managerRoom = '##MANAGE##';
var nspName = '/live';
var localMetricsData = require('../util/localCache.js');

const ApiNames = {
    PrefixUrl: url => `/api/${url}`,
    MetricGet: 'metrics/get',
    MetricGetDataFreshness: metricName => `metrics/${metricName}/freshness`
};

function getRoomForSchema(schema) {
    return schema || 'Default';
}

var schemaHandlers = require('./schemaHandlers');
const redisProxy = require('./dataProxy/redisProxy');

function forSchema(schema, successCallback) {
    var handler = schemaHandlers.forSchema(schema);
    if (handler) {
        successCallback(handler);
    } else {
        log.error('unsupported schema:"' + schema + '"');
    }
}

var redis = require('../util/redisclient');

function initialize(app, server) {
    function getOperation(service, operation) {
        app.get(ApiNames.PrefixUrl(service), (req, res, next) => respond(operation(req), res));
    }

    function postOperation(service, operation) {
        app.post(ApiNames.PrefixUrl(service), (req, res, next) => respond(operation(req), res));
    }

    if (config.env.redisData && !config.env.enableLocalOneBox) {
        let redisClient = redis(config.env.redisData.server);

        var isLiveOn = false;

        if (config.env.isLiveOn) {
            initLive(server);
            isLiveOn = true;
        }

        app.get('/api/feature/liveon', (req, res) => {
            if (isLiveOn) {
                res.end('live is already on');
            } else {
                //TODO: update to 1.1.0 of appinsight package to take latest fixes.
                initLive(server);
                isLiveOn = true;
                res.end('live is on');
            }
        });

        app.get(ApiNames.PrefixUrl(ApiNames.MetricGet), (req, res) => {
            let metricName = req.query.m;
            let startTime = req.query.s;
            let endTime = req.query.e;
            if (!metricName) res.end('metricname could not be null');
            else if (!startTime) res.end('starttime could not be null');
            else {
                redisClient.zrangebyscore(metricName, startTime, endTime, (err, result) => {
                    if (err) {
                        log.error(err);
                        res.status(500).send({ error: err });
                    } else {
                        res.send(result);
                        res.end();
                    }
                });
            }
        });

        getOperation(ApiNames.MetricGetDataFreshness(':name'), req => db.getProductByName(req.params.name));
    }

    if (config.env.enableLocalOneBox) {
        localMetricsData.init(app, server);

        app.get(ApiNames.PrefixUrl(ApiNames.MetricGet), (req, res) => {
            let metricName = req.query.m;
            let startTime = req.query.s;
            let endTime = req.query.e;
            if (!metricName) res.end('metricname could not be null');
            else if (!startTime) res.end('starttime could not be null');
            else {
                let localData = localMetricsData.getLocalData(metricName, startTime, endTime);
                res.send(localData);
                res.end();
            }
        });
        getOperation(ApiNames.MetricGetDataFreshness(':name'), req => db.getProductByName(req.params.name));
    }
}

module.exports = {
    init: initialize
};
