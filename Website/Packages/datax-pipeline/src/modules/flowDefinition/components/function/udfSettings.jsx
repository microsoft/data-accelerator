// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import * as Helpers from '../../flowHelpers';
import { TextField, Label, DefaultButton } from 'office-ui-fabric-react';
import { Colors, IconButtonStyles } from 'datax-common';

export default class UdfSettings extends React.Component {
    constructor(props) {
        super(props);
    }

    render() {
        // Customize jarFile text box based on whether its running in OneBox mode or cloud mode
        let jarFileTextBox = this.props.enableLocalOneBox ? (
            <TextField
                className="ms-font-m"
                spellCheck={false}
                label="Local Path URI to JAR File"
                placeholder="e.g. /app/aspnetcore/jars/udf.jar"
                value={this.props.functionItem.properties.path}
                onChange={(event, value) => this.props.onUpdateUdfPath(value)}
            />
        ) : (
            <TextField
                type="password"
                className="ms-font-m"
                spellCheck={false}
                label="Azure Blob Storage Path to JAR File"
                placeholder="e.g. https://<storage_account_name>.blob.core.windows.net/<container_name>/<jar_file_path>"
                value={this.props.functionItem.properties.path}
                onChange={(event, value) => this.props.onUpdateUdfPath(value)}
            />
        );

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

                <div style={sectionStyle}>{jarFileTextBox}</div>

                <div style={sectionStyle}>
                    <TextField
                        className="ms-font-m info-settings-textbox"
                        spellCheck={false}
                        label="Class Name"
                        placeholder={`class containing your ${this.props.functionDisplayName} implementation`}
                        value={this.props.functionItem.properties.class}
                        onChange={(event, value) => this.props.onUpdateUdfClass(value)}
                    />
                </div>

                {this.renderDependencyLibs()}
            </div>
        );
    }

    renderDependencyLibs() {
        const libs = this.props.functionItem.properties.libs.map((item, index) => {
            return this.renderDependencyLib(item, index, this.props.functionItem.properties.libs.length);
        });

        return (
            <div>
                <Label className="ms-font-m" style={titleStyle}>
                    Dependency Libraries (JAR Files)
                </Label>

                <div style={libListContainerStyle}>
                    {libs}
                    <div>{this.renderAddDependencyLibButton()}</div>
                </div>
            </div>
        );
    }

    renderDependencyLib(item, index) {
        // Customize dependencyLib text box based on whether its running in OneBox mode or cloud mode

        let dependencyLibTextBox = this.props.enableLocalOneBox ? (
            <TextField
                className="ms-font-m"
                spellCheck={false}
                placeholder="e.g. /app/aspnetcore/lib.jar"
                value={item}
                onChange={(event, value) => this.onUpdateDependencyLib(value, index)}
            />
        ) : (
            <TextField
                type="password"
                className="ms-font-m"
                spellCheck={false}
                placeholder="e.g. https://<storage_account_name>.blob.core.windows.net/<container_name>/<jar_file_path>"
                value={item}
                onChange={(event, value) => this.onUpdateDependencyLib(value, index)}
            />
        );

        return (
            <div key={`dependency_lib_${index}`} style={libRowStyle}>
                <div style={libPathStyle}>{dependencyLibTextBox}</div>
                {this.renderDeleteDependencyLibButton(index)}
            </div>
        );
    }

    renderAddDependencyLibButton() {
        return (
            <DefaultButton
                className="rule-settings-button"
                style={addButtonStyle}
                title="Add new dependency library"
                onClick={() => this.onAddDependencyLib()}
            >
                <i style={IconButtonStyles.greenStyle} className="ms-Icon ms-Icon--Add" />
                Add Library
            </DefaultButton>
        );
    }

    renderDeleteDependencyLibButton(index) {
        return (
            <DefaultButton
                key={`delete_dependency_lib_${index}`}
                className="rule-settings-delete-button"
                style={deleteButtonStyle}
                title="Delete dependency library"
                onClick={() => this.onDeleteDependencyLib(index)}
            >
                <i style={IconButtonStyles.neutralStyle} className="ms-Icon ms-Icon--Delete" />
            </DefaultButton>
        );
    }

    onAddDependencyLib() {
        const libs = [...this.props.functionItem.properties.libs, ''];
        this.props.onUpdateUdfDependencyLibs(libs);
    }

    onDeleteDependencyLib(index) {
        const libs = [...this.props.functionItem.properties.libs];
        libs.splice(index, 1);
        this.props.onUpdateUdfDependencyLibs(libs);
    }

    onUpdateDependencyLib(value, index) {
        const libs = [...this.props.functionItem.properties.libs];
        libs[index] = value;
        this.props.onUpdateUdfDependencyLibs(libs);
    }

    validateProperty(value) {
        if (value === '') return '';
        return !Helpers.isNumberAndStringOnly(value) ? 'Letters, numbers, and underscores only' : '';
    }
}

// Props
UdfSettings.propTypes = {
    functionItem: PropTypes.object.isRequired,
    functionDisplayName: PropTypes.string.isRequired,

    onUpdateFunctionName: PropTypes.func.isRequired,
    onUpdateUdfPath: PropTypes.func.isRequired,
    onUpdateUdfClass: PropTypes.func.isRequired,
    onUpdateUdfDependencyLibs: PropTypes.func.isRequired
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

const libListContainerStyle = {
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

const libRowStyle = {
    display: 'flex',
    flexDirection: 'row'
};

const libPathStyle = {
    flex: 1
};

const addButtonStyle = {
    marginBottom: 15
};

const deleteButtonStyle = {
    marginBottom: 15,
    marginRight: 15
};
