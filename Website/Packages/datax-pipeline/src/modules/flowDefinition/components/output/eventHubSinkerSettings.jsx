// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import * as Helpers from '../../flowHelpers';
import * as Models from '../../flowModels';
import { TextField, Label, ComboBox } from 'office-ui-fabric-react';

export default class EventHubSinkerSettings extends React.Component {
    constructor(props) {
        super(props);
    }

    render() {
        return (
            <div style={rootStyle}>
                <div style={sectionStyle}>
                    <TextField
                        className="ms-font-m info-settings-textbox"
                        spellCheck={false}
                        label="Alias"
                        value={this.props.sinker.id}
                        onChange={(event, value) => this.props.onUpdateSinkerName(value)}
                        onGetErrorMessage={value => this.validateProperty(value)}
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

                <div style={sectionStyle}>
                    <TextField
                        type="password"
                        className="ms-font-m"
                        spellCheck={false}
                        label="Connection String"
                        value={this.props.sinker.properties.connectionString}
                        onChange={(event, value) => this.props.onUpdateEventHubConnection(value)}
                        autoAdjustHeight
                        resizable={false}
                    />
                </div>

                {this.renderFormatTypeDropdown()}
                {this.renderCompressionTypeDropdown()}
            </div>
        );
    }

    renderFormatTypeDropdown() {
        const options = Models.sinkerFormatTypes.map(type => {
            return {
                key: type.key,
                text: type.name,
                disabled: type.disabled
            };
        });

        return (
            <div style={sectionStyle}>
                <Label className="ms-font-m info-settings-textbox">Format</Label>
                <ComboBox
                    className="ms-font-m info-settings-textbox"
                    options={options}
                    selectedKey={this.props.sinker.properties.format}
                    onChange={(event, selection) => this.props.onUpdateFormatType(selection.key)}
                />
            </div>
        );
    }

    renderCompressionTypeDropdown() {
        const options = Models.sinkerCompressionTypes.map(type => {
            return {
                key: type.key,
                text: type.name,
                disabled: type.disabled
            };
        });

        return (
            <div style={sectionStyle}>
                <Label className="ms-font-m info-settings-textbox">Compression</Label>
                <ComboBox
                    className="ms-font-m info-settings-textbox"
                    options={options}
                    selectedKey={this.props.sinker.properties.compressionType}
                    onChange={(event, selection) => this.props.onUpdateCompressionType(selection.key)}
                />
            </div>
        );
    }

    validateProperty(value) {
        if (value === '') return '';
        return !Helpers.isNumberAndStringOnly(value) ? 'Letters, numbers, and underscores only' : '';
    }
}

// Props
EventHubSinkerSettings.propTypes = {
    sinker: PropTypes.object.isRequired,
    sinkerDisplayName: PropTypes.string.isRequired,

    onUpdateSinkerName: PropTypes.func.isRequired,
    onUpdateEventHubConnection: PropTypes.func.isRequired,
    onUpdateFormatType: PropTypes.func.isRequired,
    onUpdateCompressionType: PropTypes.func.isRequired
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
