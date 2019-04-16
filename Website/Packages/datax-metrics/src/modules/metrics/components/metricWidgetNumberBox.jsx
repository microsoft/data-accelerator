// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import { connect } from 'react-redux';
import { withRouter } from 'react-router';
import { Colors } from 'datax-common';
import numberFormat from 'simple-number-format';
import metricService from '../metrics.polling';
import metricBuffer from '../metrics.buffer.accumulate';
import * as aggregators from '../metrics.aggregator';

function format(value) {
    return numberFormat.longint(Math.floor(value));
}

class MetricWidgetNumberBox extends React.Component {
    constructor() {
        super();
        this.state = {};
        this.metrics = metricService();
    }

    componentDidMount() {
        let pollingInterval = this.props.pollingInterval || 60000;
        let redrawInterval = this.props.redrawInterval || 100;
        let aggregator = aggregators[this.props.aggregator || 'sum'];

        let initTime = new Date();
        initTime.setHours(0, 0, 0, 0);
        this.initialTime = initTime;

        this.metrics
            .schemas(this.props.metricNames)
            .buffer(metricBuffer(pollingInterval, aggregator))
            .pollingInterval(pollingInterval)
            .dataUpdateInterval(redrawInterval)
            .initTimestamp(initTime)
            .onInitData(data => {
                this.initialTime = initTime;
                let value = aggregator(data);
                value = isNaN(value) ? undefined : value;
                //this.subTitle = 'Since ' + moment(this.initialTime).format('YYYY/MM/DD HH:mm:ss');
                this.setState({ value: value });
            })
            .onData(data => {
                let value = aggregator(data);
                let newValue = this.state.value == undefined ? value : this.state.value + value;
                this.setState({ value: newValue });
            })
            .start();
    }

    componentWillUnmount() {
        this.metrics.stop();
    }

    render() {
        return (
            <div
                style={{
                    width: 300,
                    height: 120,
                    padding: 10,
                    borderColor: Colors.customGray,
                    borderWidth: 1,
                    borderStyle: 'solid',
                    backgroundColor: Colors.white,
                    textAlign: 'center',
                    marginRight: 20
                }}
            >
                <div
                    style={{
                        height: 80,
                        fontSize: 48,
                        fontWeight: 'bolder',
                        textAlign: 'center',
                        color: Colors.black,
                        paddingTop: 10
                    }}
                >
                    {format(this.state.value)}
                </div>

                <div
                    style={{
                        bottom: 0,
                        fontSize: 14,
                        fontWeight: 'bold',
                        textAlign: 'center',
                        color: Colors.black
                    }}
                >
                    <div>{this.props.title}</div>
                    {this.subTitle && (
                        <div
                            style={{
                                fontStyle: 'italic',
                                fontWeight: 'normal',
                                fontSize: 12
                            }}
                        >
                            {this.subTitle}
                        </div>
                    )}
                </div>
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
    )(MetricWidgetNumberBox)
);
