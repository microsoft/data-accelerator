// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
function noop() {}

function service() {
    var _dataAccessor = null;
    var _onConfig = noop,
        _onInitData = [],
        _onData = [];
    var _pollingInterval = 60000;
    var _transitionPeriod = 5000;
    var _initTimestamp = undefined;
    var _series = undefined;

    var lastTime = undefined;
    var inPolling = false;

    function initLastTime() {
        if (_initTimestamp == undefined) {
            var now = new Date();
            lastTime = new Date(now.getTime() - _pollingInterval);
        } else {
            lastTime = _initTimestamp;
        }
    }

    function initPoll() {
        inPolling = true;
        initLastTime();
        if (_onInitData.length > 0) {
            var time = new Date();
            time.setMilliseconds(time.getMilliseconds() - _pollingInterval);
            _dataAccessor(lastTime, time).then(results => {
                lastTime = time;
                if (inPolling) {
                    _onInitData.forEach(cb => cb(results, time));
                    poll();
                }
            });
        } else {
            poll();
        }
    }

    function poll() {
        if (!inPolling) return;
        if (_onData.length > 0) {
            var time = new Date();
            _dataAccessor(lastTime, time).then(results => {
                lastTime = time;
                if (inPolling) {
                    _onData.forEach(cb => cb(results, time));
                    setTimeout(poll, _pollingInterval - _transitionPeriod);
                }
            });
        }
    }

    function start() {
        if (!inPolling) {
            initPoll();
        }
    }

    function stop() {
        inPolling = false;
    }

    var rthost = {
        start: start,
        stop: stop,
        onConfig: cb => {
            _onConfig = cb;
            return rthost;
        },
        onInitData: cb => {
            _onInitData.push(cb);
            return rthost;
        },
        onData: cb => {
            _onData.push(cb);
            return rthost;
        },
        dataAccessor: cb => {
            _dataAccessor = cb;
            return rthost;
        },
        pollingInterval: s => {
            if (s === undefined) return _pollingInterval;
            else {
                _pollingInterval = s;
                return rthost;
            }
        },
        initTimestamp: t => {
            if (t === undefined) return _initTimestamp;
            _initTimestamp = t;
            return rthost;
        },
        series: s => {
            if (s === undefined) return _series;
            _series = s;
            return rthost;
        }
    };

    return rthost;
}

export default service;
