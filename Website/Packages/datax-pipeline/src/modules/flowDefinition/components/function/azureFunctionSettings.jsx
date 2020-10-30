// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import * as Helpers from '../../flowHelpers';
import * as Models from '../../flowModels';
import { TextField, Label, DefaultButton, ComboBox } from 'office-ui-fabric-react';
import { Colors, IconButtonStyles } from 'datax-common';

export default class AzureFunctionSettings extends React.Component {
    constructor(props) {
        super(props);
    }

    render() {
        const options = Models.functionMethodTypes.map(type => {
            return {
                key: type.key,
                text: type.name,
                disabled: type.disabled
            };
        });

        return (
            <div style={rootStyle}>
                <div style={sectionStyle}>
                    <TextField
                        className="ms-font-m info-settings-textbox"
                        spellCheck={false}
                        label="Alias"
                        value={this.props.functionItem.id}
                        onChange={(event, value) => this.props.onUpdateFunctionName(value)}
                        onGetErrorMessage={value => this.validateProperty(value)}
                    />
                </div>

                <div style={functionTypeSection}>
                    <TextField
                        className="ms-font-m info-settings-textbox"
                        spellCheck={false}
                        label="Function Type"
                        disabled={true}
                        value={this.props.functionDisplayName}
                    />
                </div>

                <div style={sectionStyle}>
                    <TextField
                        className="ms-font-m info-settings-textbox"
                        spellCheck={false}
                        label="Base URL"
                        placeholder="e.g. https://<function_app_name>.azurewebsites.net"
                        value={this.props.functionItem.properties.serviceEndpoint}
                        onChange={(event, value) => this.props.onUpdateAzureFunctionServiceEndpoint(value)}
                    />
                </div>

                <div style={sectionStyle}>
                    <TextField
                        className="ms-font-m info-settings-textbox"
                        spellCheck={false}
                        label="API"
                        placeholder="e.g. <api_name>"
                        value={this.props.functionItem.properties.api}
                        onChange={(event, value) => this.props.onUpdateAzureFunctionApi(value)}
                    />
                </div>

                <div style={sectionStyle}>
                    <TextField
                        type="password"
                        className="ms-font-m info-settings-textbox"
                        spellCheck={false}
                        label="Function Key"
                        placeholder="optional but recommended"
                        value={this.props.functionItem.properties.code}
                        onChange={(event, value) => this.props.onUpdateAzureFunctionCode(value)}
                    />
                </div>

                <div style={sectionStyle}>
                    <Label className="ms-font-m info-settings-textbox">Method Type</Label>
                    <ComboBox
                        className="ms-font-m info-settings-textbox"
                        options={options}
                        selectedKey={this.props.functionItem.properties.methodType}
                        onChange={(event, selection) => this.props.onUpdateAzureFunctionMethodType(selection.key)}
                    />
                </div>

                {this.renderParams()}
            </div>
        );
    }

    renderParams() {
        const libs = this.props.functionItem.properties.params.map((item, index) => {
            return this.renderParam(item, index, this.props.functionItem.properties.params.length);
        });

        return (
            <div>
                <Label className="ms-font-m" style={titleStyle}>
                    Parameters
                </Label>

                <div style={paramListContainerStyle}>
                    {libs}
                    <div>{this.renderAddParamButton()}</div>
                </div>
            </div>
        );
    }

    renderParam(item, index) {
        return (
            <div key={`param_${index}`} style={paramRowStyle}>
                <div style={paramNameStyle}>
                    <TextField
                        className="ms-font-m"
                        spellCheck={false}
                        placeholder="parameter name"
                        value={item}
                        onChange={(event, value) => this.onUpdateParam(value, index)}
                    />
                </div>

                {this.renderDeleteParamButton(index)}
            </div>
        );
    }

    renderAddParamButton() {
        return (
            <DefaultButton
                className="rule-settings-button"
                style={addButtonStyle}
                title="Add new parameter"
                onClick={() => this.onAddParam()}
            >
                <i style={IconButtonStyles.greenStyle} className="ms-Icon ms-Icon--Add" />
                Add Parameter
            </DefaultButton>
        );
    }

    renderDeleteParamButton(index) {
        return (
            <DefaultButton
                key={`delete_param_${index}`}
                className="rule-settings-delete-button"
                style={deleteButtonStyle}
                title="Delete parameter"
                onClick={() => this.onDeleteParam(index)}
            >
                <i style={IconButtonStyles.neutralStyle} className="ms-Icon ms-Icon--Delete" />
            </DefaultButton>
        );
    }

    onAddParam() {
        const params = [...this.props.functionItem.properties.params, ''];
        this.props.onUpdateAzureFunctionParams(params);
    }

    onDeleteParam(index) {
        const params = [...this.props.functionItem.properties.params];
        params.splice(index, 1);
        this.props.onUpdateAzureFunctionParams(params);
    }

    onUpdateParam(value, index) {
        const params = [...this.props.functionItem.properties.params];
        params[index] = value;
        this.props.onUpdateAzureFunctionParams(params);
    }

    validateProperty(value) {
        if (value === '') return '';
        return !Helpers.isNumberAndStringOnly(value) ? 'Letters, numbers, and underscores only' : '';
    }
}

// Props
AzureFunctionSettings.propTypes = {
    functionItem: PropTypes.object.isRequired,
    functionDisplayName: PropTypes.string.isRequired,

    onUpdateAzureFunctionServiceEndpoint: PropTypes.func.isRequired,
    onUpdateAzureFunctionApi: PropTypes.func.isRequired,
    onUpdateAzureFunctionCode: PropTypes.func.isRequired,
    onUpdateAzureFunctionMethodType: PropTypes.func.isRequired,
    onUpdateAzureFunctionParams: PropTypes.func.isRequired
};

// Styles
const rootStyle = {
    paddingLeft: 30,
    paddingRight: 30,
    paddingBottom: 30
};

const functionTypeSection = {
    paddingBottom: 40
};

const sectionStyle = {
    paddingBottom: 15
};

const titleStyle = {
    marginTop: 5,
    marginBottom: 2
};

const paramListContainerStyle = {
    display: 'flex',
    flexDirection: 'column',
    backgroundColor: Colors.neutralLighter,
    border: `1px solid ${Colors.neutralQuaternaryAlt}`,
    paddingLeft: 15,
    paddingRight: 0,
    paddingTop: 25,
    paddingBottom: 10,
    marginBottom: 20
};

const paramRowStyle = {
    display: 'flex',
    flexDirection: 'row'
};

const paramNameStyle = {
    flex: 1
};

const addButtonStyle = {
    marginBottom: 15
};

const deleteButtonStyle = {
    marginBottom: 15,
    marginRight: 15
};
