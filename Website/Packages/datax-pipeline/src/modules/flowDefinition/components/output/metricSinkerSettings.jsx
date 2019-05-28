// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import { TextField } from 'office-ui-fabric-react';
import { Colors, StatementBox } from 'datax-common';

export default class MetricSinkerSettings extends React.Component {
    constructor(props) {
        super(props);
    }

    render() {
        return (
            <div style={rootStyle}>
                <StatementBox
                    icon="BIDashboard"
                    statement="Dedicated output sink for you to use to log metrics that will be displayed in the Metrics dashboard."
                    overrideRootStyle={statementBoxSectionStyle}
                    style={statementBoxStyle}
                />

                <div style={sectionStyle}>
                    <TextField
                        className="ms-font-m info-settings-textbox"
                        spellCheck={false}
                        label="Alias"
                        disabled={true}
                        value={this.props.sinker.id}
                    />
                </div>

                <div style={sinkTypeSection}>
                    <TextField
                        className="ms-font-m info-settings-textbox"
                        spellCheck={false}
                        label="Sink Type"
                        disabled={true}
                        value={this.props.sinkerDisplayName}
                    />
                </div>

                {this.renderMetricDashboardLink()}
            </div>
        );
    }

    renderMetricDashboardLink() {
        // show metrics dashboard link only when flow has previously been saved in the past
        if (this.props.flowId) {
            return (
                <a style={linkStyle} href={`${window.origin}/dashboard/${this.props.flowId}`} target="_blank" rel="noopener noreferrer">
                    Metrics Dashboard
                </a>
            );
        } else {
            return null;
        }
    }
}

// Props
MetricSinkerSettings.propTypes = {
    flowId: PropTypes.string,
    sinker: PropTypes.object.isRequired,
    sinkerDisplayName: PropTypes.string.isRequired
};

// Styles
const rootStyle = {
    paddingLeft: 30,
    paddingRight: 30,
    paddingBottom: 30
};

const sinkTypeSection = {
    paddingBottom: 40
};

const sectionStyle = {
    paddingBottom: 15
};

const statementBoxSectionStyle = {
    paddingTop: 12,
    paddingBottom: 30
};

const statementBoxStyle = {
    border: `1px solid ${Colors.neutralTertiaryAlt}`,
    backgroundColor: Colors.white
};

const linkStyle = {
    fontSize: 14,
    lineHeight: '29px',
    color: Colors.themePrimary
};
