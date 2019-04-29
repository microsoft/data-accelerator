// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import * as Helpers from '../../flowHelpers';
import * as Models from '../../flowModels';
import { Label, TextField, Toggle, Dropdown, DefaultButton } from 'office-ui-fabric-react';
import { JsonEditor } from 'jsoneditor-react';
import 'jsoneditor-react/es/editor.min.css';
import ace from 'brace';
import 'brace/mode/json';
import 'brace/theme/textmate';
import MonacoEditor from 'react-monaco-editor';
import 'brace/mode/sql';
import 'brace/theme/xcode';
import { Colors, IconButtonStyles, ScrollableContentPane, StatementBox, LoadingPanel, getApiErrorMessage } from 'datax-common';

const inputSchemaExampleWiki = 'https://aka.ms/data-accelerator-input';
const normalizationExampleWiki = 'https://aka.ms/data-accelerator-normalization';

export default class InputSettingsContent extends React.Component {
    constructor(props) {
        super(props);

        this.state = {
            showNormalizationSnippet: false,
            error: {}
        };
    }

    render() {
        return (
            <div style={rootStyle}>
                <StatementBox
                    icon="Build"
                    statement="Define the input data that will be used by your processing query script. This is referenced as DataXProcessedInput in your query."
                />
                {this.renderContent()}
            </div>
        );
    }

    renderContent() {
        return (
            <div style={{ display: 'flex', flexDirection: 'column', flex: 1 }}>
                <div style={contentStyle}>
                    {this.renderLeftPane()}
                    {this.renderRightPane()}
                </div>
            </div>
        );
    }

    renderLeftPane() {
        return (
            <div style={leftPaneStyle}>
                <ScrollableContentPane backgroundColor={Colors.neutralLighterAlt}>
                    <div style={leftPaneSectionStyle}>
                        {this.renderModeDropdown()}
                        {this.renderTypeDropdown()}
                        {this.renderEventHubName()}
                        {this.renderEventHubConnection()}
                        {this.renderSubscriptionId()}
                        {this.renderResourceGroup()}
                    </div>

                    <div style={dividerStyle} />

                    <div style={leftPaneSectionStyle}>
                        <div style={sectionStyle}>
                            <TextField
                                className="ms-font-m"
                                label="Batch Interval in Seconds"
                                value={this.props.input.properties.windowDuration}
                                onChange={(event, value) => this.props.onUpdateWindowDuration(value)}
                                onGetErrorMessage={value => this.validateNumber(value)}
                                disabled={!this.props.inputWindowDurationTextboxEnabled}
                            />
                        </div>

                        <div style={sectionStyle}>
                            <TextField
                                className="ms-font-m"
                                label="Maximum Events per Batch Interval"
                                value={this.props.input.properties.maxRate}
                                onChange={(event, value) => this.props.onUpdateMaxRate(value)}
                                onGetErrorMessage={value => this.validateNumber(value)}
                                disabled={!this.props.inputMaxRateTextboxEnabled}
                            />
                        </div>

                        {this.renderTimestampColumn()}
                        {this.renderWatermark()}
                    </div>

                    <div style={dividerStyle} />

                    <div style={leftPaneSectionStyle}>{this.renderShowNormalizationSnippetToggle()}</div>
                </ScrollableContentPane>
            </div>
        );
    }

    renderRightPane() {
        return (
            <div style={rightPaneStyle}>
                {this.renderInputSchemaEditor()}
                {this.renderNormalizationEditor()}
            </div>
        );
    }

    renderModeDropdown() {
        const options = Models.inputModes.map(mode => {
            return {
                key: mode.key,
                text: mode.name,
                disabled: mode.disabled
            };
        });

        return (
            <div style={typeDropdownStyle}>
                <Label className="ms-font-m">Mode</Label>
                <Dropdown
                    className="ms-font-m"
                    options={options}
                    selectedKey={this.props.input.mode}
                    onChange={(event, selection) => this.props.onUpdateMode(selection.key)}
                    disabled={!this.props.inputModeDropdownEnabled}
                />
            </div>
        );
    }

    renderTypeDropdown() {
        const options = this.props.enableLocalOneBox
            ? Models.inputTypes
                  .filter(type => type.name === 'Local')
                  .map(type => {
                      return {
                          key: type.key,
                          text: type.name,
                          disabled: type.disabled
                      };
                  })
            : Models.inputTypes
                  .filter(type => type.name !== 'Local')
                  .map(type => {
                      return {
                          key: type.key,
                          text: type.name,
                          disabled: type.disabled
                      };
                  });

        return (
            <div style={typeDropdownStyle}>
                <Label className="ms-font-m">Type</Label>
                <Dropdown
                    className="ms-font-m"
                    options={options}
                    selectedKey={this.props.input.type}
                    onChange={(event, selection) => this.props.onUpdateType(selection.key)}
                    disabled={!this.props.inputTypeDropdownEnabled}
                />
            </div>
        );
    }

    renderEventHubName() {
        if (this.props.input.type === Models.inputTypeEnum.iothub) {
            return (
                <div style={sectionStyle}>
                    <TextField
                        className="ms-font-m"
                        spellCheck={false}
                        label="Event Hub-Compatible Name"
                        value={this.props.input.properties.inputEventhubName}
                        onChange={(event, value) => this.props.onUpdateHubName(value)}
                        disabled={!this.props.inputEventHubEnabled}
                    />
                </div>
            );
        } else {
            return null;
        }
    }

    renderEventHubConnection() {
        if (this.props.input.type === Models.inputTypeEnum.local) {
            return null;
        } else {
            const label = this.props.input.type === Models.inputTypeEnum.iothub ? 'Event Hub-Compatible Endpoint' : 'Connection String';
            return (
                <div style={sectionStyle}>
                    <TextField
                        type="password"
                        className="ms-font-m"
                        spellCheck={false}
                        label={label}
                        value={this.props.input.properties.inputEventhubConnection}
                        onChange={(event, value) => this.props.onUpdateHubConnection(value)}
                        autoAdjustHeight
                        resizable={false}
                        disabled={!this.props.inputEventHubConnectionStringEnabled}
                    />
                </div>
            );
        }
    }

    renderSubscriptionId() {
        if (this.props.input.type === Models.inputTypeEnum.local) {
            return null;
        } else {
            return (
                <div style={sectionStyle}>
                    <TextField
                        type="password"
                        className="ms-font-m"
                        spellCheck={false}
                        label="Subscription Id"
                        value={this.props.input.properties.inputSubscriptionId}
                        onChange={(event, value) => this.props.onUpdateSubscriptionId(value)}
                        autoAdjustHeight
                        resizable={false}
                        disabled={!this.props.inputEventHubConnectionStringEnabled} // reuse connection enabled - they relate tp this setting
                        placeholder="use default"
                    />
                </div>
            );
        }
    }

    renderResourceGroup() {
        if (this.props.input.type === Models.inputTypeEnum.local) {
            return null;
        } else {
            return (
                <div style={sectionStyle}>
                    <TextField
                        type="password"
                        className="ms-font-m"
                        spellCheck={false}
                        label="Resource Group Name"
                        value={this.props.input.properties.inputResourceGroup}
                        onChange={(event, value) => this.props.onUpdateResourceGroup(value)}
                        autoAdjustHeight
                        resizable={false}
                        disabled={!this.props.inputEventHubConnectionStringEnabled} // reuse connection enabled - they relate to this setting
                        placeholder="use default"
                    />
                </div>
            );
        }
    }

    renderTimestampColumn() {
        return (
            <div style={sectionStyle}>
                <TextField
                    className="ms-font-m"
                    spellCheck={false}
                    label="Timestamp Column for Windowing"
                    value={this.props.input.properties.timestampColumn}
                    onChange={(event, value) => this.props.onUpdateTimestampColumn(value)}
                    disabled={!this.props.inputTimestampColumnEnabled}
                />
            </div>
        );
    }

    renderWatermark() {
        return (
            <div>
                <Label className="ms-font-m">Wait Time for Late Arriving Data</Label>

                <div style={watermarkContainerStyle}>
                    <div style={watermarkValueStyle}>
                        <TextField
                            className="ms-font-m"
                            value={this.props.input.properties.watermarkValue}
                            onChange={(event, value) => this.props.onUpdateWatermarkValue(value)}
                            onGetErrorMessage={value => this.validateWatermarkValue(value)}
                            disabled={!this.props.inputWatermarkEnabled}
                        />
                    </div>

                    {this.renderWatermarkUnitDropdown()}
                </div>
            </div>
        );
    }

    renderWatermarkUnitDropdown() {
        const options = Models.watermarkUnits.map(type => {
            return {
                key: type.key,
                text: type.name,
                disabled: type.disabled
            };
        });

        return (
            <div style={watermarkUnitDropdownStyle}>
                <Dropdown
                    className="ms-font-m"
                    options={options}
                    selectedKey={this.props.input.properties.watermarkUnit}
                    onChange={(event, selection) => this.props.onUpdateWatermarkUnit(selection.key)}
                    disabled={!this.props.inputWatermarkEnabled}
                />
            </div>
        );
    }

    renderShowNormalizationSnippetToggle() {
        return (
            <div style={toggleSectionStyle}>
                <Toggle
                    onText="Show Normalization"
                    offText="Show Normalization"
                    checked={this.state.showNormalizationSnippet}
                    onChange={(event, value) => this.setState({ showNormalizationSnippet: value })}
                />
            </div>
        );
    }

    renderGetInputSchemaButton() {
        const display = 'Get Schema';
        const enableButton =
            this.props.input.properties.inputEventhubConnection !== '' &&
            !this.props.fetchingInputSchema &&
            this.props.getInputSchemaButtonEnabled;

        return (
            <div style={rightSideSettingsStyle}>
                <DefaultButton
                    key={display}
                    className="query-pane-button"
                    title={display}
                    disabled={!enableButton}
                    onClick={this.onGetInputSchema.bind(this)}
                >
                    <i
                        style={enableButton ? IconButtonStyles.greenStyle : IconButtonStyles.disabledStyle}
                        className="ms-Icon ms-Icon--Embed"
                    />
                    {display}
                </DefaultButton>
                <div style={toggleStyle}>
                    <Label className="ms-font-m">Duration in seconds</Label>
                </div>
                <div style={toggleStyle}>
                    <TextField
                        className="query-pane-TextField ms-font-m"
                        spellCheck={false}
                        value={this.props.samplingInputDuration}
                        onChange={(event, value) => this.props.onUpdateSamplingInputDuration(value)}
                        onGetErrorMessage={value => this.validateNumber(value)}
                    />
                </div>
            </div>
        );
    }

    renderInputSchemaEditor() {
        let editor;
        if (this.props.fetchingInputSchema) {
            const timer = parseInt(this.props.samplingInputDuration) - this.props.timer;
            const label = timer > -1 ? `Sampling Data... ${timer}` : 'Generating schema...';

            editor = <LoadingPanel showImmediately={true} message={label} style={spinnerContainerStyle} />;
        } else {
            const value =
                this.props.input.properties.inputSchemaFile !== '{}' ? this.props.input.properties.inputSchemaFile : this.state.error;

            editor = (
                <div style={jsonEditorContainerStyle} className="editor-container">
                    <JsonEditor
                        ace={ace}
                        mode={this.props.inputSchemaEditorEnabled ? 'code' : 'view'}
                        theme="ace/theme/textmate"
                        allowedModes={this.props.inputSchemaEditorEnabled ? ['code', 'tree'] : ['view']}
                        value={this.getSchemaAsJsonObject(value)}
                        onChange={value => this.onJsonSchemaChange(value)}
                    />
                </div>
            );
        }

        return (
            <div style={rightPaneTopContentStyle}>
                <div>
                    {this.renderGetInputSchemaButton()}

                    <Label className="ms-font-m" style={inlineBlockStyle}>
                        Describe Schema in JSON Format
                    </Label>
                    <a style={linkStyle} href={inputSchemaExampleWiki} target="_blank">
                        View Example
                    </a>
                </div>
                {editor}
            </div>
        );
    }

    renderNormalizationEditor() {
        if (!this.state.showNormalizationSnippet) {
            return null;
        }

        return (
            <div style={rightPaneBottomContentStyle}>
                <div>
                    <Label className="ms-font-m" style={inlineBlockStyle}>
                        Data Schema Normalization (input SQL to run against the schema above)
                    </Label>
                    <a style={linkStyle} href={normalizationExampleWiki} target="_blank">
                        View Example
                    </a>
                </div>

                <div style={editorContainerStyle}>
                    <MonacoEditor
                        name="normalizationeditor"
                        height="100%"
                        width="100%"
                        fontSize="13px"
                        language="sql"
                        value={this.props.input.properties.normalizationSnippet}
                        onChange={snippet => this.props.onUpdateNormalizationSnippet(snippet)}
                        options={{
                            selectOnLineNumbers: true,
                            roundedSelection: false,
                            cursorStyle: 'line',
                            automaticLayout: true,
                            readOnly: !this.props.inputNormalizationEditorEnabled
                        }}
                    />
                </div>
            </div>
        );
    }

    validateNumber(value) {
        return !Helpers.isValidNumberAboveZero(value) ? 'Numbers only and must be greater than zero' : '';
    }

    validateWatermarkValue(value) {
        return value === '' || !Helpers.isValidNumberAboveOrEqualZero(value) ? 'Numbers only and must be zero or greater' : '';
    }

    getSchemaAsJsonObject(jsonString) {
        try {
            return JSON.parse(jsonString);
        } catch (error) {
            return jsonString;
        }
    }

    // Note: the JSON editor will only trigger this event on valid JSON
    onJsonSchemaChange(jsonObject) {
        const jsonString = JSON.stringify(jsonObject);
        this.props.onUpdateSchema(jsonString);
    }

    onGetInputSchema() {
        this.props
            .onGetInputSchema()
            .then(result => {
                this.setState({ error: {} });
            })
            .catch(error => {
                this.setState({ error: JSON.stringify({ Error: getApiErrorMessage(error) }) });
            });
    }
}

// Props
InputSettingsContent.propTypes = {
    input: PropTypes.object.isRequired,
    timer: PropTypes.number.isRequired,
    samplingInputDuration: PropTypes.string.isRequired,
    onGetInputSchema: PropTypes.func.isRequired,
    onUpdateMode: PropTypes.func.isRequired,
    onUpdateType: PropTypes.func.isRequired,
    onUpdateHubName: PropTypes.func.isRequired,
    onUpdateHubConnection: PropTypes.func.isRequired,
    onUpdateSubscriptionId: PropTypes.func.isRequired,
    onUpdateResourceGroup: PropTypes.func.isRequired,
    onUpdateWindowDuration: PropTypes.func.isRequired,
    onUpdateTimestampColumn: PropTypes.func.isRequired,
    onUpdateWatermarkValue: PropTypes.func.isRequired,
    onUpdateWatermarkUnit: PropTypes.func.isRequired,
    onUpdateMaxRate: PropTypes.func.isRequired,
    onUpdateSchema: PropTypes.func.isRequired,
    onUpdateNormalizationSnippet: PropTypes.func.isRequired,
    onUpdateSamplingInputDuration: PropTypes.func.isRequired,
    getInputSchemaButtonEnabled: PropTypes.bool.isRequired,
    inputModeDropdownEnabled: PropTypes.bool.isRequired,
    inputTypeDropdownEnabled: PropTypes.bool.isRequired,
    inputEventHubEnabled: PropTypes.bool.isRequired,
    inputEventHubConnectionStringEnabled: PropTypes.bool.isRequired,
    inputWindowDurationTextboxEnabled: PropTypes.bool.isRequired,
    inputMaxRateTextboxEnabled: PropTypes.bool.isRequired,
    inputTimestampColumnEnabled: PropTypes.bool.isRequired,
    inputWatermarkEnabled: PropTypes.bool.isRequired,
    inputSchemaEditorEnabled: PropTypes.bool.isRequired,
    inputNormalizationEditorEnabled: PropTypes.bool.isRequired
};

// Styles
const rootStyle = {
    display: 'flex',
    flexDirection: 'column',
    overflowY: 'hidden',
    height: '100%'
};

const contentStyle = {
    display: 'flex',
    flexDirection: 'row',
    overflowY: 'hidden',
    flex: 1 //test
};

const leftPaneStyle = {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    borderRight: `1px solid ${Colors.neutralTertiaryAlt}`
};

const leftPaneSectionStyle = {
    paddingLeft: 30,
    paddingRight: 30
};

const dividerStyle = {
    borderBottom: `1px solid ${Colors.neutralTertiaryAlt}`,
    marginTop: 15,
    marginBottom: 15
};

const rightPaneStyle = {
    flex: 3,
    display: 'flex',
    flexDirection: 'column',
    paddingBottom: 30
};

const rightPaneTopContentStyle = {
    flex: 3,
    paddingLeft: 30,
    paddingRight: 30,
    paddingTop: 20,
    display: 'flex',
    flexDirection: 'column',
    overflowY: 'hidden'
};

const rightPaneBottomContentStyle = {
    flex: 2,
    paddingLeft: 30,
    paddingRight: 30,
    paddingTop: 20,
    display: 'flex',
    flexDirection: 'column',
    overflowY: 'hidden'
};

const jsonEditorContainerStyle = {
    flex: 1,
    height: '100%',
    overflow: 'hidden',
    display: 'flex',
    flexDirection: 'column'
};

const editorContainerStyle = {
    display: 'flex',
    flexDirection: 'column',
    flex: 1,
    border: `1px solid ${Colors.neutralTertiaryAlt}`
};

const sectionStyle = {
    paddingBottom: 15
};

const toggleSectionStyle = {
    paddingTop: 10,
    paddingBottom: 15
};

const typeDropdownStyle = {
    paddingBottom: 15
};

const inlineBlockStyle = {
    display: 'inline-block'
};

const linkStyle = {
    paddingLeft: 10,
    fontSize: 14,
    float: 'right',
    lineHeight: '29px',
    color: Colors.themePrimary
};

const watermarkContainerStyle = {
    display: 'flex',
    flexDirection: 'row'
};

const watermarkValueStyle = {
    flex: 1,
    marginRight: 10
};

const watermarkUnitDropdownStyle = {
    flex: 1,
    paddingBottom: 15
};

const rightSideSettingsStyle = {
    display: 'flex',
    flexDirection: 'row'
};

const toggleStyle = {
    paddingLeft: 5
};

const spinnerContainerStyle = {
    border: `1px solid ${Colors.neutralQuaternaryAlt}`
};
