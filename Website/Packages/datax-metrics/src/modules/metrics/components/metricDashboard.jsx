// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import { connect } from 'react-redux';
import { withRouter } from 'react-router';
import { renderWidget } from './metricWidgetGeneric';
import metricsDatasource from '../metrics.datasource';
import ServiceGroup from '../serviceGroup';
import * as Api from '../api';
import { LoadingPanel, StatementBox } from 'datax-common';
import { Constants } from '../apiConstants';

class MetricDashboard extends React.Component {
    constructor() {
        super();

        this.state = {
            loading: true,
            jobsAllRunning: false
        };
    }

    bumpMetrics(product) {
        if (product && product.metrics && product.metrics.sources) {
            return new ServiceGroup(
                product.metrics.sources
                    .filter(dataSource => dataSource && typeof dataSource === 'object')
                    .map(dataSource => metricsDatasource(dataSource, dataSource.name + '_', state => this.setState(state)))
            );
        } else {
            return new ServiceGroup();
        }
    }

    componentDidMount() {
        if (
            !this.props.product ||
            !this.props.product.jobNames ||
            (this.props.product.jobNames && this.props.product.jobNames.length < 1)
        ) {
            return;
        }

        Api.allJobsRunningForProduct(this.props.product).then(jobsAllRunning => {
            this.metrics = this.bumpMetrics(this.props.product);
            this.metrics.start();
            this.setState({
                loading: false,
                jobsAllRunning: jobsAllRunning
            });
        });
    }

    componentWillUnmount() {
        if (this.metrics) {
            this.metrics.stop();
        }
    }

    render() {
        let product = this.props.product;

        if (!product) {
            return null;
        } else if (!product.jobNames || (product.jobNames && product.jobNames.length < 1)) {
            const message = 'No metrics to display. No job for the flow is created. Please start the job to see metrics.';
            return (
                <div>
                    <div style={headerStyle}>{product.displayName} Metrics</div>
                    <StatementBox icon="IncidentTriangle" overrideRootStyle={messageStyle} statement={message} />
                </div>
            );
        } else if (product.gui.input.mode === Constants.batching) {
            const message = 'Metrics is not supported for the batch job.';
            return (
                <div>
                    <div style={headerStyle}>{product.displayName} Metrics</div>
                    <StatementBox icon="IncidentTriangle" overrideRootStyle={messageStyle} statement={message} />
                </div>
            );
        } else if (this.state.loading) {
            return <LoadingPanel showImmediately={true} />;
        } else if (!this.state.jobsAllRunning) {
            const message =
                product.jobNames.length === 1
                    ? 'No metrics to display. The job for the flow is not running. Please start the job to see metrics.'
                    : 'No metrics to display. Some of the jobs for the flow are not running. Please start all jobs to see metrics.';

            return (
                <div>
                    <div style={headerStyle}>{product.displayName} Metrics</div>
                    <StatementBox icon="IncidentTriangle" overrideRootStyle={messageStyle} statement={message} />
                </div>
            );
        } else {
            let firstRowBoxes = [],
                secondRowBoxes = [],
                timeCharts = [];

            if (product.metrics && product.metrics.widgets) {
                for (let i in product.metrics.widgets) {
                    let w = product.metrics.widgets[i];
                    if (!w || typeof w !== 'object') continue;

                    let visual = renderWidget(product.metrics.widgets[i], `widget${i}`, this.state);
                    switch (w.position) {
                        case 'FirstRow':
                            firstRowBoxes.push(visual);
                            break;
                        case 'SecondRow':
                            secondRowBoxes.push(visual);
                            break;
                        case 'TimeCharts':
                            timeCharts.push(visual);
                            break;
                        default:
                            throw new Error(`Unknown position '${w.position}'`);
                    }
                }
            }

            return (
                <div>
                    <div style={headerStyle}>{product.displayName} Metrics</div>
                    <div style={rowItemContainerStyle}>{firstRowBoxes}</div>
                    <div style={rowItemContainerStyle}>{secondRowBoxes}</div>
                    {timeCharts}
                </div>
            );
        }
    }
}

// State Props
const mapStateToProps = state => ({});

// Dispatch Props
const mapDispatchToProps = () => ({});

// Styles
const rowItemContainerStyle = {
    display: 'flex',
    flexDirection: 'row',
    paddingBottom: 20
};

const messageStyle = {
    padding: 5
};

const headerStyle = {
    fontSize: 24,
    paddingBottom: 30,
    fontWeight: 'normal'
};

export default withRouter(
    connect(
        mapStateToProps,
        mapDispatchToProps
    )(MetricDashboard)
);
