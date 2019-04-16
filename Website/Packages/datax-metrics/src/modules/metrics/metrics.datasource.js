// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import Q from 'q';
import * as Api from './api';
import ServiceGroup from './serviceGroup';
import metricService from './metrics.polling';
import metricBufferAccumulate from './metrics.buffer.accumulate';
import * as aggregators from './metrics.aggregator';

const getYValue = d => d[1];
const aggregate = (data, aggregator) => aggregator(data.map(d => aggregator(d.map(getYValue))));

function emptyArray(itemCount) {
    var ret = [];
    for (var i = 0; i < itemCount; i++) ret.push([]);
    return ret;
}

function chartDataFactory(series, maxItemCount) {
    let seriesCount = series.length;
    var data = {
        series: series,
        x: emptyArray(seriesCount),
        y: emptyArray(seriesCount)
    };

    return {
        add: (ys, time) => {
            ys.forEach((d, i) => {
                if (!isNaN(d)) {
                    if (data.x[i].length > maxItemCount) {
                        data.x[i].splice(0, 1);
                        data.y[i].splice(0, 1);
                    }

                    data.x[i].push(time);
                    data.y[i].push(d);
                }
            });
        },

        addMetricTimePairs: xys => {
            xys.forEach((d, i) => {
                if (data.x[i] === undefined) {
                    console.error(`series #${i} is not initialized properly, please check data`, data);
                }

                if (data.x[i].length > maxItemCount + d.length) {
                    data.x[i].splice(0, d.length);
                    data.y[i].splice(0, d.length);
                }

                d.forEach(v => {
                    data.x[i].push(v[0]);
                    data.y[i].push(v[1]);
                });
            });
        },

        get: () => Object.assign({}, data) //clone
    };
}

class UpdateService {
    constructor(updateInterval) {
        this.updateInterval = updateInterval;
        this.isUpdating = false;
        this.subscribers = [];
        this.update = this.update.bind(this);
    }

    update() {
        if (!this.isUpdating) return;
        var now = new Date();
        this.subscribers.forEach(s => s(now));
        setTimeout(this.update, this.updateInterval);
    }

    onUpdate(cb) {
        this.subscribers.push(cb);
        return this;
    }

    start() {
        if (!this.isUpdating) {
            this.isUpdating = true;
            this.update();
        }
    }

    stop() {
        this.isUpdating = false;
    }
}

function dataSplitter(updateInterval) {
    var dataSeries = null;
    var coverWindow = 60000;
    var offset = coverWindow + coverWindow;
    var isUpdating = false;
    var _onData = () => {};

    var updater = new UpdateService(updateInterval);
    updater.onUpdate(now => {
        var time = new Date(now.getTime() - offset);
        if (dataSeries) _onData(dataSeries.map(d => getData(d, time)), time);
    });

    function getInitialTick(d) {
        return d[0].getTime() - coverWindow;
    }

    function getData(dataSery, time) {
        if (dataSery.lastTick == null) return [];
        var dataBuffer = dataSery.data;
        if (dataBuffer.length == 0) return [];
        var ret = [];
        var tick = dataSery.lastTick,
            targetTick = time.getTime();
        var i = 0;
        while (i < dataBuffer.length) {
            var d = dataBuffer[i];
            var startTick = getInitialTick(d);
            var endTick = startTick + coverWindow;
            if (targetTick < startTick) {
                break;
            } else if (targetTick < endTick) {
                while (tick <= targetTick) {
                    ret.push([new Date(tick), d[1]]);
                    tick += updateInterval;
                }

                break;
            } else {
                while (tick <= endTick) {
                    ret.push([new Date(tick), d[1]]);
                    tick += updateInterval;
                }

                i++;
            }
        }

        if (i > 0) {
            dataBuffer.splice(0, i);
        }

        dataSery.lastTick = tick;
        return ret;
    }

    var service = {
        feed: (data, time) => {
            if (dataSeries == null) {
                var initialTick = new Date().getTime() - coverWindow;
                dataSeries = data.map(d => {
                    if (d.length > 0) {
                        d.sort((a, b) => a[0] > b[0]);
                        return { lastTick: getInitialTick(d[0]), data: d };
                    }

                    return { lastTick: null, data: [] };
                });
            } else
                data.forEach((d, i) => {
                    if (d.length > 0) {
                        var buffer = dataSeries[i].data;
                        buffer.push.apply(buffer, d);
                        buffer.sort((a, b) => a[0] > b[0]);
                        if (dataSeries[i].lastTick == null) dataSeries[i].lastTick = getInitialTick(buffer[0]);
                    }
                });
        },
        onData: cb => {
            _onData = cb;
            return service;
        },
        start: () => {
            updater.start();
        },
        stop: () => {
            updater.stop();
        }
    };

    return service;
}

function getSourcer(input) {
    if (!input) return;
    let inputParsed = parseInputQuery(input);
    let dataAccessor = inputParsed.query;
    if (!dataAccessor) return;

    var pollingInterval = input.pollingInterval || 60000;

    var initTime = new Date();
    initTime.setHours(0, 0, 0, 0);

    return metricService()
        .series(inputParsed.series)
        .dataAccessor(dataAccessor)
        .pollingInterval(pollingInterval)
        .initTimestamp(initTime);
}

function normalizeMetricApiQueryKeys(metricKeys) {
    const isObject = value => value !== null && typeof value === 'object';
    return metricKeys.map((metricKey, i) => (isObject(metricKey) ? metricKey : { name: metricKey, displayName: 'Shard ' + String(i + 1) }));
}

function parseInputQuery(input) {
    switch (input.type) {
        case 'MetricApi':
            var metricKeys = normalizeMetricApiQueryKeys(input.metricKeys);
            return {
                series: metricKeys,
                query: (startTime, endTime) =>
                    Q.all(
                        metricKeys.map(metric =>
                            Api.getMetricsData(metric.name, startTime.getTime(), endTime.getTime()).then(results =>
                                results.map(d => [new Date(d.uts), d.val])
                            )
                        )
                    )
            };
        case 'MetricDetailsApi':
            var metricKeys = normalizeMetricApiQueryKeys(input.metricKeys);
            return {
                series: metricKeys,
                query: (startTime, endTime) =>
                    Q.all(
                        metricKeys.map(metric =>
                            Api.getMetricsData(metric.name, startTime.getTime(), endTime.getTime()).then(results =>
                                results.map(d => ({
                                    Time: new Date(d.uts),
                                    Description: d.pivot1
                                }))
                            )
                        )
                    )
            };
        case 'MetricApiRefreshness':
            var metricKeys = normalizeMetricApiQueryKeys(input.metricKeys);
            return {
                series: metricKeys,
                query: (startTime, endTime) =>
                    Q.all(
                        metricKeys.map(metric =>
                            Api.getMetricsDataFreshness(metric.name).then(results => results.map(d => [new Date(d.uts), d.val]))
                        )
                    )
            };
        case 'Static':
            return {
                series: [{ name: 'Result 1', displayName: 'Result 1' }],
                query: (startTime, endTime) => Q(input.query)
            };
        default:
            console.error(`unknown input type:'${input.type}'`);
            throw new Exception(`unknown input type:'${input.type}'`);
    }
}

function checkOutputData(supportedDataNames, outputData) {
    if (!outputData) return;
    let outputVarNames = {};
    supportedDataNames.forEach(dn => {
        if (outputData[dn]) outputVarNames[dn] = outputData[dn];
    });

    if (Object.keys(outputVarNames).length == 0) return;
    return outputVarNames;
}

function buildVarName(prefix, localVarName) {
    return `${prefix || ''}${localVarName}`;
}

function emitData(name, value, dataCallback) {
    var state = {};
    state[name] = value;
    dataCallback(state);
}

const chartDataGenerator = (state, outputConfig, emit) => {
    var chartTimeWindow = outputConfig.chartTimeWindowInMs || 5 * 60 * 1000;
    var chartInterval = outputConfig.chartInterval || 1000;
    var chartData = chartDataFactory([{}], Math.round(chartTimeWindow / chartInterval));

    return new UpdateService(chartInterval).onUpdate(now => {
        if (state.dataBuffer == null) return;
        chartData.addMetricTimePairs(state.dataBuffer.map(d => (d == null ? [] : [[now, d[1]]])));
        emit(chartData.get());
    });
};

const outputTypes = {
    SumWithTimeChart: (sourcer, output, varPrefix, dataCallback) => {
        let outputVarNames = checkOutputData(['sum', 'timechart', 'average'], output.data);
        if (!outputVarNames) return;

        var redrawInterval = output.updateInterval || 100;
        var aggregator = aggregators.sum;
        var valueSum = 0;

        const stateNameSum = buildVarName(varPrefix, 'sum'),
            stateNameTimechart = buildVarName(varPrefix, 'timechart'),
            stateNameAverage = buildVarName(varPrefix, 'average'),
            stateNameCurrentSpeed = buildVarName(varPrefix, 'speed');

        var lastAvgUpdateTime = null;
        var chartTimeWindow = output.chartTimeWindowInMs || 5 * 60 * 1000;
        var chartInterval = output.chartInterval || 1000;
        var dataBuffer = null;
        var maxDataCount = chartTimeWindow / chartInterval;
        var chartData = chartDataFactory(sourcer.series(), maxDataCount);

        var lastEventsChartingTime = undefined;
        function addToBuffer(data, time) {
            data.forEach((d, i) => {
                if (!isNaN(d)) {
                    if (isNaN(dataBuffer[i])) dataBuffer[i] = d;
                    else dataBuffer[i] += d;
                }
            });
        }

        function bumpBuffer(time) {
            var acc = 0;
            chartData.add(dataBuffer.map(d => (isNaN(d) ? acc : (acc += d))), time);
            dataBuffer = dataBuffer.map(d => NaN);
            return chartData.get();
        }

        let initTime = sourcer.initTimestamp(),
            pollingInterval = sourcer.pollingInterval();

        // If dynamicOffset is defined, use it to calculate the average
        if(output.dynamicOffsetInMs){
            initTime = new Date();
            initTime.setMilliseconds(initTime.getMilliseconds() - output.dynamicOffsetInMs);
            sourcer.initTimestamp(initTime);
        }

        let firstBatchTime = null;
        const minuteInMs = 60000;

        function onRawDataUpdate(input, time) {
            var speeds = input.map(s => {
                if (s.length > 0) {
                    var lastValue = s[s.length - 1][1];
                    return Math.floor(lastValue / (pollingInterval / minuteInMs));
                } else return NaN;
            });

            var speed = aggregator(speeds);

            if (!isNaN(speed)) {
                let state = {};
                state[stateNameCurrentSpeed] = speed;

                if (lastEventsChartingTime == undefined) {
                    lastEventsChartingTime = time;
                    addToBuffer(speeds.map(s => s / 60), time);
                    state[stateNameTimechart] = bumpBuffer(time);
                }

                dataCallback(state);
            }
        }

        return metricBufferAccumulate(sourcer, pollingInterval, aggregator)
            .updateInterval(redrawInterval)
            .onInternalData(onRawDataUpdate)
            .onInternalInitData(onRawDataUpdate)
            .onInitData((data, time, firstEventTime) => {
                 //Initialization
                dataBuffer = data.map(d => NaN);
                var value = aggregator(data);
                if (isNaN(value)) return;
                valueSum += value;
                firstBatchTime = firstEventTime || initTime;

                var elapsedTimeInMinutes = (time - firstBatchTime) / minuteInMs; // convert into minutes
                var avg = Math.round(valueSum / elapsedTimeInMinutes);               

                var state = {};
                state[stateNameSum] = Math.floor(valueSum);
                state[stateNameAverage] = avg;
                dataCallback(state);
                lastAvgUpdateTime = time;
            })
            .onData((data, time) => {
                var value = aggregator(data);
                if (isNaN(value)) return;
                valueSum += value;

                let state = {};
                state[stateNameSum] = Math.floor(valueSum);

                addToBuffer(data, time);
                if (lastEventsChartingTime == undefined) {
                    lastEventsChartingTime = time;
                } else if (time - lastEventsChartingTime > chartInterval) {
                    state[stateNameTimechart] = bumpBuffer(time);
                    lastEventsChartingTime = time;
                }

                if (time - lastAvgUpdateTime > minuteInMs) {
                    var elapsedTimeInMinutes = (new Date() - initTime) / minuteInMs;
                    var avg = Math.round(valueSum / elapsedTimeInMinutes);
                    lastAvgUpdateTime = time;
                    state[stateNameAverage] = avg;
                }

                dataCallback(state);
            });
    },
    AverageWithTimeChart: (sourcer, output, varPrefix, dataCallback) => {
        const aggregator = aggregators.average;
        var splitter = null;
        var count = 0;
        var average = NaN;

        const generators = {
            timechart: emit => {
                var chartTimeWindow = output.chartTimeWindowInMs || 5 * 60 * 1000;
                var chartInterval = output.chartInterval || 1000;
                var chartData = chartDataFactory(sourcer.series(), Math.round(chartTimeWindow / chartInterval));

                var initTime = new Date();
                initTime.setMilliseconds(initTime.getMilliseconds() - chartTimeWindow);
                sourcer.initTimestamp(initTime);

                splitter = dataSplitter(chartInterval).onData((data, time) => {
                    chartData.addMetricTimePairs(data);
                    emit(chartData.get());
                });

                return splitter;
            },
            average: emit => {
                return sourcer
                    .onInitData((data, time) => {
                        if (splitter) splitter.feed(data, time);

                        var value = aggregate(data, aggregator);
                        if (isNaN(value)) return;
                        emit(value);

                        count = data.reduce((s, d) => s + d.length, 0);
                    })
                    .onData((data, time) => {
                        if (splitter) splitter.feed(data, time);
                        let newAvg = aggregate(data, aggregator);
                        let thisCount = data.reduce((s, d) => s + d.length, 0);
                        if (!isNaN(newAvg)) {
                            average = isNaN(average) ? newAvg : (newAvg * thisCount + count * average) / (count + thisCount);
                            count += thisCount;

                            emit(average);
                        }
                    });
            }
        };

        let outputVarNames = checkOutputData(Object.keys(generators), output.data);
        if (!outputVarNames) return;

        return new ServiceGroup(
            Object.keys(outputVarNames).map(k => {
                let varName = buildVarName(varPrefix, k);
                return generators[k](d => emitData(varName, d, dataCallback));
            })
        );
    },
    LatestWithTimeChart: (sourcer, output, varPrefix, dataCallback) => {
        const aggregator = aggregators.first;
        var dataBuffer = null;
        const generators = {
            timechart: emit => {
                var chartTimeWindow = output.chartTimeWindowInMs || 5 * 60 * 1000;
                var chartInterval = output.chartInterval || 1000;
                var chartData = chartDataFactory([{}], Math.round(chartTimeWindow / chartInterval));

                return new UpdateService(chartInterval).onUpdate(now => {
                    if (dataBuffer == null) return;
                    chartData.addMetricTimePairs(dataBuffer.map(d => (d == null ? [] : [[now, d[1]]])));
                    emit(chartData.get());
                });
            },
            current: emit => {
                return sourcer
                    .onInitData((data, time) => {
                        var value = aggregate(data, aggregator);
                        if (isNaN(value)) return;
                        emit(value);

                        dataBuffer = data.map(d => {
                            if (d.length == 0) return null;
                            return d[d.length - 1];
                        });
                    })
                    .onData((data, time) => {
                        emit(aggregate(data, aggregator));

                        data.forEach((d, i) => {
                            if (d.length == 0) return;
                            dataBuffer[i] = d[d.length - 1];
                        });
                    });
            }
        };

        let outputVarNames = checkOutputData(Object.keys(generators), output.data);
        if (!outputVarNames) return;

        return new ServiceGroup(
            Object.keys(outputVarNames).map(k => {
                let varName = buildVarName(varPrefix, k);
                return generators[k](d => emitData(varName, d, dataCallback));
            })
        );
    },
    SimpleSum: (sourcer, output, varPrefix, dataCallback) => {
        let outputVarNames = checkOutputData(['sum'], output.data);
        if (!outputVarNames) return;

        var redrawInterval = output.updateInterval || 100;
        var aggregator = aggregators.sum;
        var output_count = 0;
        const stateVarName = buildVarName(varPrefix, 'sum');
        var dataHandler = (data, time) => {
            var value = aggregator(data);
            if (isNaN(value)) value = 0;
            output_count += value;
            emitData(stateVarName, Math.floor(output_count), dataCallback);
        };
        return metricBufferAccumulate(sourcer, sourcer.pollingInterval(), aggregator)
            .updateInterval(redrawInterval)
            .onInitData(dataHandler)
            .onData(dataHandler);
    },
    Latest: (sourcer, output, varPrefix, dataCallback) => {
        const supportedDataNames = ['current'];
        if (!output.data) return;
        let outputVarNames = Object.keys(output.data).filter(n => supportedDataNames.indexOf(n) >= 0);
        if (outputVarNames.length == 0) return;

        var aggregator = aggregators.max;
        const stateVarName = buildVarName(varPrefix, 'current');
        return sourcer.onData((data, time) => {
            var value = aggregate(data, aggregator);
            var newValue = isNaN(value) ? NaN : value;
            emitData(stateVarName, isNaN(value) ? NaN : value, dataCallback);
        });
    },
    DirectTimeChart: (sourcer, output, varPrefix, dataCallback) => {
        var dataBuffer = null;
        const generators = {
            timechart: emit => {
                var chartTimeWindow = output.chartTimeWindowInMs || 5 * 60 * 1000;
                var chartInterval = output.chartInterval || 1000;

                var initTime = new Date();
                initTime.setMilliseconds(initTime.getMilliseconds() - chartTimeWindow);
                sourcer.initTimestamp(initTime);

                var chartData = chartDataFactory(sourcer.series(), Math.round(chartTimeWindow / chartInterval));
                const update = (data, time) => {
                    chartData.addMetricTimePairs(data);
                    emit(chartData.get());
                };

                return sourcer.onInitData(update).onData(update);
            }
        };

        let outputVarNames = checkOutputData(Object.keys(generators), output.data);
        if (!outputVarNames) return;

        return new ServiceGroup(
            Object.keys(outputVarNames).map(k => {
                let varName = buildVarName(varPrefix, k);
                return generators[k](d => emitData(varName, d, dataCallback));
            })
        );
    },
    DirectTable: (sourcer, output, varPrefix, dataCallback) => {
        var dataBuffer = null;
        const generators = {
            table: emit => {
                var chartTimeWindow = output.chartTimeWindowInMs || 5 * 60 * 1000;

                var initTime = new Date();
                initTime.setMilliseconds(initTime.getMilliseconds() - chartTimeWindow);
                sourcer.initTimestamp(initTime);
                var tableData = sourcer.series().map(s => []);
                const update = (data, time) => {
                    tableData.forEach((td, i) => {
                        for (var j in data[i]) {
                            td.push(data[i][j]);
                            if (td.length > 10) td.shift();
                        }
                    });

                    emit(tableData);
                };
                return sourcer.onInitData(update).onData(update);
            }
        };

        let outputVarNames = checkOutputData(Object.keys(generators), output.data);
        if (!outputVarNames) return;

        return new ServiceGroup(
            Object.keys(outputVarNames).map(k => {
                let varName = buildVarName(varPrefix, k);
                return generators[k](d => emitData(varName, d, dataCallback));
            })
        );
    }
};

function service(dataSource, varPrefix, dataCallback) {
    if (!dataSource || !dataSource.output || !dataSource.input) return;
    let sourcer = getSourcer(dataSource.input);
    if (!sourcer) return;

    let output = dataSource.output;
    if (!output || !output.type) return;
    let serviceGenerator = outputTypes[output.type];
    if (!serviceGenerator) return;

    return serviceGenerator(sourcer, output, varPrefix, dataCallback);
}

export default service;
