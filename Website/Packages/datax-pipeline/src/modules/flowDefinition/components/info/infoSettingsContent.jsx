// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import { DefaultButton, TextField } from 'office-ui-fabric-react';
import { Colors, IconButtonStyles, ScrollableContentPane, StatementBox } from 'datax-common';

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

                {this.props.isDatabricksSparkType && (
                    <div style={rowFlexSettingsStyle}>
                        <TextField
                            type="password"
                            className="ms-font-m info-settings-textbox"
                            spellCheck={false}
                            label="Databricks Token"
                            placeholder="Enter Token and Click Save"
                            disabled={false}
                            value={this.props.databricksToken}
                            onChange={(event, databricksToken) => this.props.onUpdateDatabricksToken(databricksToken)}
                        />
                        <div style={rightSideButtonStyle}>
                            {this.renderSaveButton()}
                        </div>
                    </div>
                )}

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

    renderSaveButton() {
        //Will be enabled when user has access and the token is not empty
        const enableButton = this.props.saveFlowButtonEnabled && this.props.databricksToken;
        return (
            <DefaultButton
                key="save"
                className="header-button"
                disabled={!enableButton}
                title="Save the Flow"
                onClick={() => this.props.saveFlowAndInitializeKernel()}
            >
                <i
                    style={enableButton ? IconButtonStyles.greenStyle : IconButtonStyles.disabledStyle}
                    className="ms-Icon ms-Icon--Save"
                />
                Save
            </DefaultButton>
        );
    }
}

// Props
InfoSettingsContent.propTypes = {
    displayName: PropTypes.string.isRequired,
    name: PropTypes.string.isRequired,
    owner: PropTypes.string.isRequired,
    onUpdateDisplayName: PropTypes.func.isRequired,
    onUpdateDatabricksToken: PropTypes.func.isRequired,
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

const rowFlexSettingsStyle = {
    display: 'flex',
    flexDirection: 'row',
    paddingBottom: 15
};

const rightSideButtonStyle = {
    paddingLeft: 5,
    paddingTop: 28
};