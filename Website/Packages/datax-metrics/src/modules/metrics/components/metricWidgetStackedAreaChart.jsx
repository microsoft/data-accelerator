// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import { connect } from 'react-redux';
import { withRouter } from 'react-router';
import Plotly from 'plotly.js';
import metricService from '../metrics.polling';
import metricBuffer from '../metrics.buffer.accumulate';
import * as aggregators from '../metrics.aggregator';
import { Colors } from 'datax-common';

class MetricWidgetStackedAreaChart extends React.Component {
    constructor() {
        super();
    }

    componentWillReceiveProps(nextProps) {
        if (!nextProps || !nextProps.value || !this.graphDiv) {
            return;
        }

        let chartData = nextProps.value;

        if (this.isPlotted) {
            Plotly.update(this.graphDiv, chartData);
        } else {
            Plotly.plot(
                this.graphDiv,
                chartData.x.map((x, i) => ({
                    x: x,
                    y: chartData.y[i],
                    name: 'Shard' + String(i + 1),
                    fill: 'tonexty',
                    mode: 'none'
                })),
                {
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
        let aggregator = aggregators[this.props.aggregator || 'sum'];
        let poller = metricService()
            .pollingInterval(pollingInterval)
            .schemas(this.props.metricNames);

        let chartData = {
            x: this.props.metricNames.map(() => []),
            y: this.props.metricNames.map(() => [])
        };

        let maxDataCount = timeWindow / redrawInterval;
        this.metrics = metricBuffer(poller, pollingInterval, aggregator)
            .updateInterval(redrawInterval)
            .onInitData(() => {
                Plotly.plot(
                    this.graphDiv,
                    this.props.metricNames.map((m, i) => ({
                        x: chartData.x[i],
                        y: chartData.y[i],
                        name: 'Shard' + String(i + 1),
                        fill: 'tonexty',
                        mode: 'none'
                    })),
                    {
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
                if (initTime == undefined) {
                    initTime = time;
                }

                let acc = 0;
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
                    acc += Math.round(d);
                    ys.push(acc);
                });

                Plotly.update(this.graphDiv, chartData);
            })
            .start();
    }

    render() {
        return (
            <div>
                <h3 style={{ fontWeight: 'normal' }}>{this.props.displayName}</h3>
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

// State Props
const mapStateToProps = () => ({});

// Dispatch Props
const mapDispatchToProps = () => ({});

export default withRouter(
    connect(
        mapStateToProps,
        mapDispatchToProps
    )(MetricWidgetStackedAreaChart)
);
