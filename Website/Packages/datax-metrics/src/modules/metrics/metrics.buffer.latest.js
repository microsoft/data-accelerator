// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
function noop() {}

module.exports = function(poller, distributionWindow, aggregator) {
    var dataBuffer = undefined;
    var _updateInterval = 1000;
    var _onData = noop;
    var _onInitData = noop;

    function onData(input, time) {
        var temp = input.map(d => aggregator(d.map(v => v[1])));
        if (dataBuffer === undefined) {
            dataBuffer = temp;
        } else {
            for (var i = 0; i < temp.length; i++) {
                if (!isNaN(temp[i])) dataBuffer[i] = temp[i];
            }
        }

        return dataBuffer;
    }

    poller.onData(onData).onInitData(onData);

    function getData(time) {
        return dataBuffer;
    }

    function update() {
        if (!isUpdating) return;
        var now = new Date();
        _onData(getData(now), now);
        setTimeout(update, _updateInterval);
    }

    poller.onData(onData).onInitData((data, time) => {
        onData(data, time);
        _onInitData(getData(time), time);
        isUpdating = true;
        update();
    });

    function reset() {
        dataBuffer = undefined;
    }

    var service = {
        reset: reset,
        start: function() {
            reset();
            poller.start();
        },
        stop: function() {
            isUpdating = false;
            poller.stop();
        },

        onInitData: v => {
            _onInitData = v;
            return service;
        },
        onData: v => {
            _onData = v;
            return service;
        },
        updateInterval: v => {
            _updateInterval = v;
            return service;
        }
    };

    return service;
};
