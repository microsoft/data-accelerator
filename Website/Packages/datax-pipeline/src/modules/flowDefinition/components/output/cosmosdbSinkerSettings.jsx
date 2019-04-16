// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import * as Helpers from '../../flowHelpers';
import { TextField } from 'office-ui-fabric-react';

export default class CosmosDbSinkerSettings extends React.Component {
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
                        onChange={(event, value) => this.props.onUpdateCosmosDbConnection(value)}
                        autoAdjustHeight
                        resizable={false}
                    />
                </div>

                <div style={sectionStyle}>
                    <TextField
                        className="ms-font-m info-settings-textbox"
                        spellCheck={false}
                        label="Database"
                        value={this.props.sinker.properties.db}
                        onChange={(event, value) => this.props.onUpdateCosmosDbDatabase(value)}
                        onGetErrorMessage={value => this.validateProperty(value)}
                    />
                </div>

                <div style={sectionStyle}>
                    <TextField
                        className="ms-font-m info-settings-textbox"
                        spellCheck={false}
                        label="Collection"
                        value={this.props.sinker.properties.collection}
                        onChange={(event, value) => this.props.onUpdateCosmosDbCollection(value)}
                        onGetErrorMessage={value => this.validateProperty(value)}
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
CosmosDbSinkerSettings.propTypes = {
    sinker: PropTypes.object.isRequired,
    sinkerDisplayName: PropTypes.string.isRequired,

    onUpdateSinkerName: PropTypes.func.isRequired,
    onUpdateCosmosDbConnection: PropTypes.func.isRequired,
    onUpdateCosmosDbDatabase: PropTypes.func.isRequired,
    onUpdateCosmosDbCollection: PropTypes.func.isRequired
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
