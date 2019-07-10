// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import * as Helpers from '../../flowHelpers';
import * as Models from '../../flowModels';
import { TextField, Label, Dropdown, Toggle } from 'office-ui-fabric-react';
import { Colors } from 'datax-common';

export default class SqlSinkerSettings extends React.Component {
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
                        label="JDBC Connection String"
                        value={this.props.sinker.properties.connectionString}
                        onChange={(event, value) => this.props.onUpdateSqlConnection(value)}
                        autoAdjustHeight
                        resizable={false}
                    />
                </div>

                <div style={sectionStyle}>
                    <TextField
                        className="ms-font-m info-settings-textbox"
                        spellCheck={false}
                        label="Table Name"
                        value={this.props.sinker.properties.tableName}
                        onChange={(event, value) => this.props.onUpdateSqlTableName(value)}
                    />
                </div>

                {this.renderSqlWriteModeDropdown()}
                {this.renderSqlBulkInsert()}
            </div>
        );
    }

    renderSqlWriteModeDropdown() {
        const isbulkInsert = this.props.sinker.properties.useBulkInsert;
        const options = Models.sinkerSqlWriteModes.map(type => {
            return {
                key: type.key,
                text: type.name,
                disabled: type.disabled
            };
        });

        return (
            <div style={sectionStyle}>
                <Label className="ms-font-m info-settings-textbox">Write Mode</Label>
                <Dropdown
                    className="ms-font-m info-settings-textbox"
                    options={options}
                    selectedKey={this.props.sinker.properties.writeMode}
                    onChange={(event, selection) => this.props.onUpdateSqlWriteMode(selection.key)}
                    disabled={isbulkInsert}
                />
            </div>
        );
    }

    renderSqlBulkInsert() {
        const isbulkInsert = this.props.sinker.properties.useBulkInsert;
        return (
            <div>
                <div style={toggleSectionStyle}>
                    <Toggle
                        label="Use SQL Bulk Insert?"
                        onText="Yes"
                        offText="No"
                        checked={isbulkInsert}
                        onChange={(event, value) => this.props.onUpdateSqlUseBulkInsert(value)}
                    />
                </div>
            </div>
        );
    }

    validateProperty(value) {
        if (value === '') return '';
        return !Helpers.isNumberAndStringOnly(value) ? 'Letters, numbers, and underscores only' : '';
    }
}

// Props
SqlSinkerSettings.propTypes = {
    sinker: PropTypes.object.isRequired,
    sinkerDisplayName: PropTypes.string.isRequired,

    onUpdateSinkerName: PropTypes.func.isRequired,
    onUpdateSqlConnection: PropTypes.func.isRequired,
    onUpdateSqlTableName: PropTypes.func.isRequired,
    onUpdateSqlWriteMode: PropTypes.func.isRequired,
    onUpdateSqlUseBulkInsert: PropTypes.func.isRequired
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

const toggleSectionStyle = {
    paddingBottom: 29,
    paddingRight: 15,
    width: 200,
    minWidth: 200
};
