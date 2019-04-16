// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
function noop() {}

function bufferOne(poller, distributionWindow) {
    var dataBuffer = null;
    var dataBase = null;

    function cutData(progress) {
        var delta = progress * dataBuffer;
        delta *= 0.95 + Math.random() * 0.1;
        if (delta > dataBuffer) delta = dataBuffer;
        dataBuffer -= delta;
        dataBase += delta;
        return delta;
    }

    var lastTick = 0;
    var endTick = 0;

    function onData(input, time) {
        var tick = time.getTime();
        input.forEach((d, i) => {
            if (endTick <= tick) {
                endTick = tick + distributionWindow;
            } else if (endTick <= tick + distributionWindow) {
                endTick += distributionWindow;
            }

            if (dataBuffer) dataBuffer += d;
            else dataBuffer = d;
        });

        return true;
    }
}

function randomRate() {
    return Math.random() * 2;
}

function buffer(poller, distributionWindow, aggregator) {
    var dataBuffer = null;
    var dataBase = null;
    var _updateInterval = 1000;
    var isUpdating = false;
    var _onData = noop;
    var _onInitData = noop;
    var _onInternalData = noop,
        _onInternalInitData = noop;

    function cutData(progress, i) {
        var r = randomRate();
        var delta = progress * dataBuffer[i] * r;
        if (delta > dataBuffer[i]) delta = dataBuffer[i];
        dataBuffer[i] -= delta;
        dataBase[i] += delta;
        return delta;
    }

    var lastTick = 0;
    var endTick = 0;

    function onData(input, time) {
        var tick = time.getTime();
        if (endTick <= tick) {
            endTick = tick + distributionWindow;
        } else if (endTick <= tick + distributionWindow) {
            endTick += distributionWindow;
        }

        var temp = input.map(d => aggregator(d.map(v => v[1])));
        for (var i = 0; i < dataBuffer.length; i++) {
            if (!isNaN(temp[i])) {
                if (isNaN(dataBuffer[i])) dataBuffer[i] = temp[i];
                else dataBuffer[i] += temp[i];
            }
        }

        _onInternalData(input, time);

        return true;
    }

    function onInitData(input) {
        var now = new Date();
        dataBase = input.map(d => aggregator(d.map(v => v[1])));
        var firstEventTime = now;
        for (var s in input) {
            if (input[s].length > 0) {
                if (firstEventTime > input[s][0][0]) firstEventTime = input[s][0][0];
            }
        }

        dataBuffer = dataBase.map(d => NaN);
        endTick = now.getTime();
        lastTick = endTick;
        _onInitData(dataBase, now, firstEventTime);

        _onInternalInitData(input, now);

        if (!isUpdating) {
            isUpdating = true;
            update();
        }
    }

    function getData(time) {
        var tick = time.getTime();
        var progress = endTick > lastTick ? (tick - lastTick) / (endTick - lastTick) : 1;
        lastTick = tick;
        return dataBuffer.map((d, i) => cutData(progress, i));
    }

    function update() {
        if (!isUpdating) return;
        var now = new Date();
        _onData(getData(now), now);
        setTimeout(update, _updateInterval);
    }

    poller.onData(onData).onInitData(onInitData);

    function reset() {
        dataBuffer = null;
        dataBase = null;
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
        },
        onInternalData: v => {
            _onInternalData = v;
            return service;
        },
        onInternalInitData: v => {
            _onInternalInitData = v;
            return service;
        }
    };
    return service;
}

module.exports = buffer;
