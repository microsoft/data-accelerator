// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import Plotly from 'plotly.js';
import metricService from '../metrics.polling';
import metricBuffer from '../metrics.buffer.latest';
import * as aggregators from '../metrics.aggregator';
import { Colors } from 'datax-common';

class MetricWidgetMultiLineChart extends React.Component {
    constructor() {
        super();
    }

    componentWillReceiveProps(nextProps) {
        if (!nextProps || !nextProps.value || !this.graphDiv) {
            return;
        }

        let valueNormalizer = nextProps.normalizer;
        let chartData = nextProps.value;
        let series = chartData.series;

        if (valueNormalizer && chartData && chartData.y) {
            chartData = {
                x: chartData.x,
                y: chartData.y.map(ys => ys.map(valueNormalizer))
            };
        }

        if (this.isPlotted) {
            Plotly.update(this.graphDiv, chartData);
        } else {
            series = series || chartData.x.map((d, i) => ({ displayName: 'Series ' + String(i + 1) }));
            Plotly.plot(
                this.graphDiv,
                series.map((s, i) => ({
                    x: chartData.x[i],
                    y: chartData.y[i],
                    name: s.displayName,
                    mode: 'lines'
                })),
                {
                    yaxis: {
                        zeroline: true,
                        showline: true,
                        showgrid: true
                    },
                    font: {
                        size: 10
                    },
                    margin: {
                        t: 0
                    }
                }
            );
            this.isPlotted = true;
        }
    }

    componentDidMount1() {
        let timeWindow = this.props.timeWindow || 5 * 60 * 1000;
        let pollingInterval = this.props.pollingInterval || 60000;
        let redrawInterval = this.props.redrawInterval || 1000;

        let initTime = undefined;
        let aggregator = aggregators[this.props.aggregator || 'average'];
        let poller = metricService()
            .pollingInterval(60000)
            .schemas(this.props.metricNames);

        let chartData = {
            x: this.props.metricNames.map(() => []),
            y: this.props.metricNames.map(() => [])
        };
        let maxDataCount = timeWindow / redrawInterval;
        let offset = this.props.offset || 0;

        let valueNormalizer = this.props.valueNormalizer || (d => d);

        this.metrics = metricBuffer(poller, pollingInterval, aggregator)
            .updateInterval(1000)
            .onInitData(() => {
                let tick = new Date().getTime() - offset;
                Plotly.plot(
                    this.graphDiv,
                    this.props.metricNames.map((m, i) => ({
                        x: chartData.x[i],
                        y: valueNormalizer(chartData.y[i]),
                        name: 'Shard' + String(i + 1),
                        mode: 'lines'
                    })),
                    {
                        yaxis: {
                            zeroline: true,
                            showline: true,
                            showgrid: true,
                            range: [0, 60]
                        },
                        xaxis: {
                            type: 'date',
                            range: [new Date(tick - timeWindow), new Date(tick)]
                        },
                        font: {
                            size: 10
                        },
                        margin: {
                            t: 0
                        }
                    }
                );
            })
            .onData((data, time) => {
                if (initTime == undefined) initTime = time;
                let tick = new Date().getTime() - offset;
                data.forEach((d, i) => {
                    if (isNaN(d)) {
                        return;
                    }

                    let xs = chartData.x[i],
                        ys = chartData.y[i];

                    if (xs.length > maxDataCount) {
                        xs.splice(0, 1);
                        ys.splice(0, 1);
                    }

                    xs.push(time);
                    ys.push(valueNormalizer(d));
                });

                Plotly.update(this.graphDiv, chartData, {
                    xaxis: {
                        range: [new Date(tick - timeWindow), new Date(tick)]
                    }
                });
            })
            .start();
    }

    render() {
        return (
            <div>
                <h3 style={{ fontWeight: 'normal', color: `${Colors.themeDarker}` }} tabindex="0">{this.props.displayName}</h3>
                <div
                    ref={div => {
                        this.graphDiv = div;
                    }}
                    style={{ width: 'auto', height: 300, paddingTop: 25, backgroundColor: Colors.white, border: `1px solid ${Colors.customGray}` }}
                />
            </div>
        );
    }
}

export default MetricWidgetMultiLineChart;
