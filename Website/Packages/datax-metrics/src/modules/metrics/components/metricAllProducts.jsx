// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import { withRouter } from 'react-router';
import metricsDataSource from '../metrics.datasource';
import ServiceGroup from '../serviceGroup';
import MetricWidgetGauge from './metricWidgetGauge';

let sourceName = 'latency';
let variableName = 'average';

class MetricAllProducts extends React.Component {
    constructor() {
        super();
        this.state = {};
    }

    componentDidMount() {
        if (!this.props.products) {
            return;
        }

        if (this.metricsStarted) {
            return;
        }

        this.metrics = this.bumpMetrics(this.props.products);
        this.metrics.start();
        this.metricsStarted = true;
    }

    bumpMetrics(products) {
        return new ServiceGroup(
            products.map(p => {
                if (p && p.metrics && p.metrics.sources) {
                    let dataSource = p.metrics.sources.find(s => s.name == sourceName);
                    if (dataSource) {
                        return metricsDataSource(dataSource, p.name + '_' + dataSource.name + '_', state => this.setState(state));
                    }
                }

                return null;
            })
        );
    }

    componentWillUnmount() {
        this.metrics.stop();
    }

    renderWidget(product, key) {
        return (
            <div key={key} style={cellStyle}>
                <MetricWidgetGauge
                    value={this.state[`${product.name}_${sourceName}_${variableName}`]}
                    showColorIndicator={true}
                    title={product.displayName}
                />
            </div>
        );
    }

    renderWidgetRow(widgets, key) {
        return (
            <div key={key} style={rowItemContainerStyle}>
                {widgets}
            </div>
        );
    }

    renderWidgetMatrix(widgets, colsCount) {
        if (!colsCount) {
            return <div>colsCount cannot be 0!</div>;
        }

        let matrix = [];
        let widgetRow = [];
        let col = 0;
        let row = 0;
        for (let i in widgets) {
            widgetRow.push(widgets[i]);
            col++;

            if (col == colsCount) {
                matrix.push(this.renderWidgetRow(widgetRow, `row${row}`));
                widgetRow = [];
                col = 0;
                row++;
            }
        }

        if (widgetRow.length > 0) {
            matrix.push(this.renderWidgetRow(widgetRow, `row${row}`));
        }

        return <div>{matrix}</div>;
    }

    render() {
        const products = this.props.products;
        const colsCount = 5;
        const widgets = products.map(p => this.renderWidget(p, p.name));

        return (
            <div>
                <div>
                    <div style={headerStyle}>Metrics</div>
                </div>
                {this.renderWidgetMatrix(widgets, colsCount)}
            </div>
        );
    }
}

// Styles
const rowItemContainerStyle = {
    display: 'flex',
    flexDirection: 'row',
    paddingBottom: 20
};

const headerStyle = {
    fontSize: 24,
    paddingBottom: 30,
    fontWeight: 'normal'
};

const cellStyle = {
    padding: 3
};

export default withRouter(MetricAllProducts);
