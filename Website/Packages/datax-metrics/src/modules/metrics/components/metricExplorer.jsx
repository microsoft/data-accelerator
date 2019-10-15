// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import { connect } from 'react-redux';
import { withRouter } from 'react-router';
import MetricDashboard from './metricDashboard';
import MetricAllProducts from './metricAllProducts';
import NavigationPanel from './navigationPanel';
import NavigationItem from './navigationItem';
import * as Actions from '../actions';
import * as Selectors from '../selectors';
import { LoadingPanel, Panel, getApiErrorMessage } from 'datax-common';
import { ColorClassNames, FontClassNames } from '@uifabric/styling';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react';

class MetricExplorer extends React.Component {
    constructor() {
        super();

        this.state = {
            showMessageBar: true,
            errorMessage: undefined
        };
    }

    componentDidMount() {
        // If call chooses to always refresh the product list on entrance to the page then do so.
        // Otherwise, get the list of products if the list has not already been fetched.
        if (this.props.alwaysRefresh || (!this.props.alwaysRefresh && this.props.products === undefined)) {
            this.props.onGetProducts().catch(error => {
                const message = getApiErrorMessage(error);
                this.setState({
                    errorMessage: message
                });
            });
        }
    }

    render() {
        return (
            <Panel>
                {this.renderMessageBar()}
                {this.renderContent()}
            </Panel>
        );
    }

    renderContent() {
        let content = <LoadingPanel showImmediately={true} />;

        let navItems;
        if (this.props.products) {
            // Get a list of products to display on side nav bar
            navItems = this.props.products.map(p => {
                return <NavigationItem key={p.name} displayName={p.displayName} link={`/dashboard/${p.name}`} />;
            });

            if (this.props.showAll) {
                // Caller chose to view all products in one single unified view
                content = <MetricAllProducts products={this.props.products} />;
            } else if (this.props.match.params && this.props.match.params.name) {
                // Caller chose to view metrics for a specific product
                let selected = this.props.products.filter(p => p.name === this.props.match.params.name);
                if (selected.length > 0) {
                    content = <MetricDashboard product={selected[0]} />;
                } else {
                    content = null; // not matches
                }
            } else if (this.props.products.length > 0) {
                // Caller did not select a product so pick the first product to display metrics
                content = <MetricDashboard product={this.props.products[0]} />;
            } else {
                content = null; // No products
            }
        }

        return (
            <div style={rootStyle}>
                <NavigationPanel>{navItems}</NavigationPanel>
                <div className={`${FontClassNames.large} ${ColorClassNames.themePrimary}`} style={contentStyle}>
                    {content}
                </div>
            </div>
        );
    }

    renderMessageBar() {
        if (this.state.errorMessage && this.state.showMessageBar) {
            return (
                <MessageBar messageBarType={MessageBarType.error} onDismiss={() => this.onDismissMessageBar()}>
                    Error - {`${this.state.errorMessage}`}
                </MessageBar>
            );
        }
    }

    onDismissMessageBar() {
        this.setState({
            showMessageBar: false
        });
    }
}

// State Props
const mapStateToProps = state => ({
    products: Selectors.getMetricProductItems(state)
});

// Dispatch Props
const mapDispatchToProps = dispatch => ({
    onGetProducts: () => dispatch(Actions.getMetricProducts())
});

// Styles
const rootStyle = {
    width: '100%',
    height: '100%',
    display: 'flex',
    flexDirection: 'row'
};

const contentStyle = {
    paddingTop: 20,
    paddingBottom: 20,
    paddingLeft: 20,
    paddingRight: 20,
    width: '100%',
    display: 'flex',
    flexDirection: 'column',
    overflowY: 'auto'
};

export default withRouter(
    connect(
        mapStateToProps,
        mapDispatchToProps
    )(MetricExplorer)
);
