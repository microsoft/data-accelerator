// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import * as Helpers from '../../flowHelpers';
import * as Models from '../../flowModels';
import { TextField, Toggle, Label, ComboBox } from 'office-ui-fabric-react';

export default class CsvReferenceDataSettings extends React.Component {
    constructor(props) {
        super(props);
    }

    render() {
        let referenceDataTextBox = this.props.enableLocalOneBox ? (
            <TextField
                className="ms-font-m"
                spellCheck={false}
                label="Local File Path URI"
                placeholder="e.g. /app/aspnetcore/refData/data.csv"
                value={this.props.referenceData.properties.path}
                onChange={(event, value) => this.props.onUpdateCsvPath(value)}
            />
        ) : (
            <TextField
                type="password"
                className="ms-font-m"
                spellCheck={false}
                label="Azure Blob Storage File Path"
                placeholder="e.g. https://<storage_account_name>.blob.core.windows.net/<container_name>/<file_path>"
                value={this.props.referenceData.properties.path}
                onChange={(event, value) => this.props.onUpdateCsvPath(value)}
            />
        );

        return (
            <div style={rootStyle}>
                <div style={sectionStyle}>
                    <TextField
                        className="ms-font-m info-settings-textbox"
                        spellCheck={false}
                        label="Alias"
                        value={this.props.referenceData.id}
                        onChange={(event, value) => this.props.onUpdateReferenceDataName(value)}
                        onGetErrorMessage={value => this.validateProperty(value)}
                    />
                </div>

                <div style={referenceDataTypeSection}>
                    <TextField
                        className="ms-font-m info-settings-textbox"
                        spellCheck={false}
                        label="Reference Data Type"
                        disabled={true}
                        value={this.props.referenceDataDisplayName}
                    />
                </div>

                <div style={sectionStyle}>{referenceDataTextBox}</div>

                {this.renderDelimiterDropdown()}
                {this.renderContainsHeaderToggle()}
            </div>
        );
    }

    renderDelimiterDropdown() {
        const options = Models.csvDelimiterTypes.map(type => {
            return {
                key: type.key,
                text: type.name,
                disabled: type.disabled
            };
        });

        return (
            <div style={sectionStyle}>
                <ComboBox
                    className="ms-font-m info-settings-textbox"
                    label="Delimiter"
                    options={options}
                    selectedKey={this.props.referenceData.properties.delimiter}
                    onChange={(event, selection) => this.props.onUpdateCsvDelimiter(selection.key)}
                />
            </div>
        );
    }

    renderContainsHeaderToggle() {
        return (
            <div style={sectionStyle}>
                <Toggle
                    label="First Row Contains Header?"
                    onText="Yes"
                    offText="No"
                    title="First Row Contains Header?"
                    checked={this.props.referenceData.properties.header}
                    onChange={(event, value) => this.props.onUpdateCsvContainsHeader(value)}
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
CsvReferenceDataSettings.propTypes = {
    referenceData: PropTypes.object.isRequired,
    referenceDataDisplayName: PropTypes.string.isRequired,

    onUpdateReferenceDataName: PropTypes.func.isRequired,
    onUpdateCsvPath: PropTypes.func.isRequired,
    onUpdateCsvDelimiter: PropTypes.func.isRequired,
    onUpdateCsvContainsHeader: PropTypes.func.isRequired
};

// Styles
const rootStyle = {
    paddingLeft: 30,
    paddingRight: 30,
    paddingBottom: 30
};

const referenceDataTypeSection = {
    paddingBottom: 40
};

const sectionStyle = {
    paddingBottom: 15
};
