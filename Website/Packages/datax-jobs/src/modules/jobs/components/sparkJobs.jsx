// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import { connect } from 'react-redux';
import { withRouter } from 'react-router';
import { PrimaryButton, SearchBox, Spinner, SpinnerSize, MessageBar, MessageBarType } from 'office-ui-fabric-react';
import SparkJobsList from './sparkJobsList';
import * as Api from '../api';
import { Colors, Panel, PanelHeader, getApiErrorMessage } from 'datax-common';

class SparkJobs extends React.Component {
    constructor() {
        super();

        this.state = {
            sparkJobs: [],
            filter: null,
            showMessageBar: false,
            errorMessage: undefined
        };
    }

    componentDidMount() {
        this.refreshItems();
    }

    render() {
        return (
            <Panel>
                <PanelHeader>Jobs</PanelHeader>
                {this.renderMessageBar()}
                {this.renderContent()}
            </Panel>
        );
    }

    renderContent() {
        if (this.state.sparkJobs.length > 0) {
            let filteredJobs = [];
            let jobs = this.state.sparkJobs;
            let filter = this.state.filter;

            if (jobs) {
                filteredJobs = filter ? jobs.filter(job => job.name.toLowerCase().indexOf(filter) > -1) : jobs;
            }

            let details;
            if (jobs) {
                details = <SparkJobsList jobs={filteredJobs} refresh={() => this.refreshItems()} />;
            } else {
                details = (
                    <div style={{ marginTop: 100 }}>
                        <Spinner size={SpinnerSize.large} label="Loading..." />
                    </div>
                );
            }

            return (
                <div style={rootStyle}>
                    <div style={contentStyle}>
                        <div style={filterContainerStyle}>
                            <SearchBox
                                className="filter-box"
                                placeholder="Filter"
                                onChanged={filterValue => this.updateFilter(filterValue)}
                            />
                            <div style={buttonStyle}>
                                <PrimaryButton text="Refresh Jobs Status" onClick={() => this.syncSparkItems()} />
                            </div>
                        </div>

                        <div style={detailsContainerStyle}>{details}</div>
                    </div>
                </div>
            );
        }
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

    setJobs(jobs) {
        this.setState({ sparkJobs: jobs });
    }

    refreshItems() {
        Api.listSparkJobs()
            .then(jobs => this.setJobs(jobs))
            .catch(error => {
                const message = getApiErrorMessage(error);
                this.setState({
                    errorMessage: message,
                    showMessageBar: true
                });
            });
    }

    syncSparkItems() {
        Api.syncSparkJobs()
            .then(jobs => this.setJobs(jobs))
            .catch(error => {
                const message = getApiErrorMessage(error);
                this.setState({
                    errorMessage: message,
                    showMessageBar: true
                });
            });
    }

    updateFilter(text) {
        this.setState({ filter: text });
    }
}

// State Props
const mapStateToProps = state => ({});

// Dispatch Props
const mapDispatchToProps = () => ({});

// Styles
const rootStyle = {
    display: 'flex',
    flexDirection: 'column',
    overflowX: 'hidden',
    overflowY: 'auto'
};

const contentStyle = {
    paddingLeft: 30,
    paddingRight: 30,
    paddingTop: 30,
    paddingBottom: 30,
    flex: 1,
    display: 'flex',
    flexDirection: 'column'
};

const filterContainerStyle = {
    paddingBottom: 15,
    display: 'flex',
    flexDirection: 'row'
};

const detailsContainerStyle = {
    backgroundColor: Colors.white,
    border: `1px solid ${Colors.neutralTertiaryAlt}`,
    flex: 1,
    overflowY: 'auto'
};

const buttonStyle = {
    paddingLeft: 10
};

export default withRouter(
    connect(
        mapStateToProps,
        mapDispatchToProps
    )(SparkJobs)
);
