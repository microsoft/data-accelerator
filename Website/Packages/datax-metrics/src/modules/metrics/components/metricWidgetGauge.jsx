// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import { Colors } from 'datax-common';
import * as d3 from 'd3';
import gauge from './d3.gauge';
import colorBox from './d3.colorBox';

function format(v) {
    if (v === undefined || v === null) {
        return 'loading...';
    } else if (isNaN(v)) {
        return v;
    } else {
        if (v < 1) return Math.floor(v * 60) + ' seconds ';
        if (v < 60) return Math.floor(v * 10) / 10 + ' minutes';
        v /= 60;
        if (v < 24) return Math.floor(v * 10) / 10 + ' hours';
        v /= 24;
        return Math.floor(v * 10) / 10 + ' hours';
    }
}

const colorScale = d3
    .scaleLinear()
    .domain([0, 0.22, 1])
    .range(['green', '#eff43e', 'red'])
    .interpolate(d3.interpolateRgb);

class MetricWidgetGauge extends React.Component {
    constructor() {
        super();
    }

    componentDidMount() {
        let title = this.props.title || 'Current Latency';

        function getColor(ratio) {
            if (isNaN(ratio)) {
                return 'gray';
            } else {
                return colorScale(ratio);
            }
        }

        this.gauge = gauge(this.graphDiv, {
            size: 200,
            clipWidth: 200,
            clipHeight: 140,
            ringWidth: 30,
            ringInset: 20,
            transitionMs: 1000,
            minValue: 0,
            maxValue: 60,
            majorTicks: 6,
            format: v => title + ': ' + format(v),
            colorScale: getColor
        }).render();

        if (this.props.showColorIndicator) {
            this.colorIndicator = colorBox(this.colorIndicatorDiv, {
                minValue: 0,
                maxValue: 60,
                colorScale: getColor
            }).render();
        }

        if (this.props.value) this.updateValue(this.props.value);
    }

    updateValue(value) {
        value /= 60;

        if (this.gauge) {
            this.gauge.update(value);
        }

        if (this.colorIndicator) {
            this.colorIndicator.update(value);
        }
    }

    componentWillReceiveProps(nextProps) {
        if (nextProps.value != this.props.value) {
            this.updateValue(nextProps.value);
        }
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
                    ref={div => {
                        this.graphDiv = div;
                    }}
                    style={{ position: 'relative' }}
                >
                    <div
                        ref={div => {
                            this.colorIndicatorDiv = div;
                        }}
                        style={{
                            width: 10,
                            height: 100,
                            position: 'absolute'
                        }}
                    />
                </div>
            </div>
        );
    }
}

export default MetricWidgetGauge;
