// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import { TextField } from 'office-ui-fabric-react';
import { Colors, ScrollableContentPane, StatementBox } from 'datax-common';

export default class InfoSettingsContent extends React.Component {
    constructor(props) {
        super(props);
    }

    render() {
        return (
            <div style={rootStyle}>
                <StatementBox icon="AsteriskSolid" statement="Basic info about your Flow." />
                <ScrollableContentPane backgroundColor={Colors.neutralLighterAlt}>{this.renderContent()}</ScrollableContentPane>
            </div>
        );
    }

    renderContent() {
        return (
            <div style={contentStyle}>
                <div style={sectionStyle}>
                    <TextField
                        className="ms-font-m info-settings-textbox"
                        spellCheck={false}
                        label="Name"
                        disabled={!this.props.flowNameTextboxEnabled}
                        value={this.props.displayName}
                        onChange={(event, displayName) => this.props.onUpdateDisplayName(displayName)}
                    />
                </div>

                <div style={sectionStyle}>
                    <TextField
                        className="ms-font-m info-settings-textbox"
                        spellCheck={false}
                        label="Creator"
                        disabled={true}
                        value={this.props.owner}
                    />
                </div>

                {this.props.name && (
                    <div style={sectionStyle}>
                        <TextField
                            className="ms-font-m info-settings-textbox"
                            spellCheck={false}
                            label="ID"
                            disabled={true}
                            value={this.props.name}
                        />
                    </div>
                )}
            </div>
        );
    }
}

// Props
InfoSettingsContent.propTypes = {
    displayName: PropTypes.string.isRequired,
    name: PropTypes.string.isRequired,
    owner: PropTypes.string.isRequired,

    onUpdateDisplayName: PropTypes.func.isRequired,
    flowNameTextboxEnabled: PropTypes.bool.isRequired
};

// Styles
const rootStyle = {
    display: 'flex',
    flexDirection: 'column',
    overflowY: 'hidden'
};

const contentStyle = {
    paddingLeft: 30,
    paddingRight: 30,
    paddingBottom: 30
};

const sectionStyle = {
    paddingBottom: 15
};
