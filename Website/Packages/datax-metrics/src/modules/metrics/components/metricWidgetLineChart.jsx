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

class MetricWidgetLineChart extends React.Component {
    constructor() {
        super();
        this.state = {};
    }

    componentDidMount() {
        let timeWindow = this.props.timeWindow || 5 * 60 * 1000;
        let pollingInterval = this.props.pollingInterval || 60000;
        let redrawInterval = this.props.redrawInterval || 1000;

        let aggregator = aggregators[this.props.aggregator || 'sum'];

        let initTime = undefined;
        this.metrics = metricService()
            .buffer(metricBuffer(pollingInterval, aggregator))
            .pollingInterval(pollingInterval)
            .dataUpdateInterval(redrawInterval)
            .onInitData(() => {
                Plotly.plot(this.graphDiv, [{ x: [], y: [] }], {
                    font: {
                        size: 10
                    },
                    margin: {
                        t: 0
                    }
                });
            })
            .onData((data, time) => {
                let value = aggregator(data);
                if (initTime == undefined) {
                    initTime = time;
                }

                let now = new Date().getTime();
                let startTime = now - timeWindow;

                if (startTime > initTime.getTime()) {
                    Plotly.relayout(this.graphDiv, {
                        xaxis: {
                            type: 'date',
                            range: [startTime, now]
                        }
                    });
                }

                Plotly.extendTraces(
                    this.graphDiv,
                    {
                        x: [[value]],
                        y: [[data]]
                    },
                    [0]
                );
            })
            .schemas(this.props.metricNames)
            .start();
    }

    componentWillUnmount() {
        this.metrics.stop();
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

// State Props
const mapStateToProps = () => ({});

// Dispatch Props
const mapDispatchToProps = () => ({});

export default withRouter(
    connect(
        mapStateToProps,
        mapDispatchToProps
    )(MetricWidgetLineChart)
);
