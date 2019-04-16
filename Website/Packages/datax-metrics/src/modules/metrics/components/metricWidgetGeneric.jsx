// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import MetricWidgetStackAreaChart from './metricWidgetStackedAreaChart';
import MetricWidgetMultiLineChart from './metricWidgetMultiLineChart';
import MetricWidgetSimpleBox from './metricWidgetSimpleBox';
import MetricWidgetPercentageBox from './metricWidgetPercentageBox';
import MetricWidgetGauge from './metricWidgetGauge';
import MetricWidgetSimpleTable from './metricWidgetSimpleTable';
import MetricWidgetDetailsList from './metricWidgetDetailsList';
import numberFormat from 'simple-number-format';

const percentagePrecision = 1000;
function percentageNumber(p, precision) {
    return Math.floor(p * precision * 100) / precision;
}

function percentage(p, precision) {
    return percentageNumber(p, precision || percentagePrecision) + '%';
}

function floatNumber(d, precision) {
    return Math.floor(d * precision) / precision;
}

const identical = d => d;
const eliminateNaN = format => d => (isNaN(d) ? '-' : format(d));
const formatterDict = numberFormat;
formatterDict.identical = identical;
formatterDict.floatNumber = d => floatNumber(d, 100);
formatterDict.percentage = percentage;

Object.keys(formatterDict).forEach(f => {
    formatterDict[f] = eliminateNaN(formatterDict[f]);
});

const percentageFormatters = {};

function percentageFormatGenerator(precision) {
    if (!(precision in percentageFormatters)) {
        percentageFormatters[precision] = eliminateNaN(d => percentage(d, precision));
    }
    return percentageFormatters[precision];
}

const normalizerDict = {
    identical: identical,
    percentage: d => d * 100
};

function getNormalizer(name) {
    return name ? normalizerDict[name] : null;
}

export function renderWidget(widget, key, state, prefix) {
    if (!widget) return <div>Null Widget</div>;

    prefix = prefix || '';
    let valueStateName = widget.data;
    if (!valueStateName) {
        return <div>Data Not Configured</div>;
    }

    let value = state[prefix + valueStateName];
    switch (widget.type) {
        case 'SimpleBox':
            let formatter = formatterDict[widget.formatter];
            return <MetricWidgetSimpleBox key={key} value={value} title={widget.displayName} format={formatter} />;

        case 'PercentageBox':
            let baseValueStateName = widget.base;
            if (!baseValueStateName) return <div>'base' parameter is not configured.</div>;
            let baseValue = state[prefix + baseValueStateName];
            return (
                <MetricWidgetPercentageBox
                    key={key}
                    value={value}
                    baseValue={baseValue}
                    title={widget.displayName}
                    valueFormat={formatterDict.longint}
                    percentageFormat={percentageFormatGenerator(widget.precision)}
                />
            );

        case 'StackAreaChart':
            return <MetricWidgetStackAreaChart key={key} value={value} displayName={widget.displayName} />;

        case 'MultiLineChart':
            let normalizer = getNormalizer(widget.normalizer);
            return (
                <MetricWidgetMultiLineChart
                    key={key}
                    value={value}
                    displayName={widget.displayName}
                    offset={widget.offset}
                    timeWindow={widget.timeWindow}
                    normalizer={normalizer}
                />
            );

        case 'Gauge':
            return <MetricWidgetGauge key={key} value={value} title={widget.displayName} />;

        case 'SimpleTable':
            return <MetricWidgetSimpleTable key={key} value={value} displayName={widget.displayName} />;

        case 'DetailsList':
            return <MetricWidgetDetailsList key={key} value={value} displayName={widget.displayName} />;

        default:
            return <div>Unknown Widget:'{widget.type}'</div>;
    }
}
