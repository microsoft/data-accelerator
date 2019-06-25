// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import SideToolBar from './sideToolBar';
import { DefaultButton, Label, TextField } from 'office-ui-fabric-react';
import { Icon } from 'office-ui-fabric-react/lib/Icon';
import brace from 'brace';
import MonacoEditorControl from './monacoeditorcontrol';
import 'brace/mode/sql';
import 'brace/theme/xcode';
import 'brace/ext/language_tools';
import { JsonEditor } from 'jsoneditor-react';
import SplitterLayout from 'react-splitter-layout';
import { Colors, IconButtonStyles, LoadingPanel, CommonHelpers as Helpers } from 'datax-common';
import * as Actions from '../queryActions';

const queryExampleWiki = 'https://aka.ms/data-accelerator-query';

export default class QuerySettingsContent extends React.Component {
    constructor(props) {
        super(props);

        this.state = {
            showCodeGenQuery: false,
            codeGenQuery: '',
            testQuery: undefined,
            fetchingCodeGenQuery: false,
            fetchingTestQuery: false,
            useDarkEditorTheme: false,
            kernelId: '',
            debugMode: false,
            completionItemProvider: undefined
        };
    }

    editorWillMount(monaco) {
        monaco.editor.props = this.props;

        // Initialize the table map
        this.props.onGetTableSchemas().then(tableToSchemaMap => {
            monaco.editor.tableToSchemaMap = tableToSchemaMap;
        });

        function createDependencyProposals(tableName) {
            let columns = monaco.editor.tableToSchemaMap[tableName].columns;
            if (!Array.isArray(columns) || columns === undefined || columns.length <= 0) return { suggestions: [] };

            let intellisenseList = [];
            for (let i = 0; i < columns.length; i++) {
                let item = {};
                item['label'] = columns[i];
                item['insertText'] = columns[i];
                intellisenseList.push(item);
            }
            return { suggestions: intellisenseList };
        }

        this.state.completionItemProvider = monaco.languages.registerCompletionItemProvider('sql', {
            provideCompletionItems: (model, position) => {
                // Refresh the table map
                monaco.editor.props.onGetTableSchemas().then(tableToSchemaMap => {
                    monaco.editor.tableToSchemaMap = tableToSchemaMap;
                });

                // Get the text until the cursor
                let textUntilPosition = model.getValueInRange({
                    startLineNumber: 1,
                    startColumn: 1,
                    endLineNumber: position.lineNumber,
                    endColumn: position.column
                });

                // Return if text is null or empty
                if (textUntilPosition === null || textUntilPosition == '') return { suggestions: [] };

                // If the last character is not '.' then bail out and let monaco do the default thing
                if (!textUntilPosition.endsWith('.')) return { suggestions: [] };

                // Find the table name
                let startTableNameIndex = textUntilPosition.length - 2;
                while (
                    startTableNameIndex >= 0 &&
                    textUntilPosition.charAt(startTableNameIndex) != ' ' &&
                    textUntilPosition.charAt(startTableNameIndex) != '\n' &&
                    textUntilPosition.charAt(startTableNameIndex) != '\r'
                )
                    startTableNameIndex--;

                let tableName = textUntilPosition.substr(startTableNameIndex, textUntilPosition.length - 1 - startTableNameIndex);
                if (tableName) {
                    tableName = tableName.trim();
                }

                // If we have information for the table return it
                if (tableName && tableName != '' && monaco.editor.tableToSchemaMap && monaco.editor.tableToSchemaMap[tableName])
                    return createDependencyProposals(tableName);
            }
        });
    }

    editorDidMount(editor) {
        this.editor = editor;
        editor.focus();
    }

    componentWillUnmount() {
        this.state.completionItemProvider.dispose();
    }

    render() {
        return <div style={rootStyle}>{this.renderContent()}</div>;
    }

    renderContent() {
        const theme = !this.state.useDarkEditorTheme ? 'vs' : 'vs-dark';
        const themeJsonEditor = 'ace/theme/jsoneditor';

        const options = {
            selectOnLineNumbers: true,
            roundedSelection: false,
            cursorStyle: 'line',
            automaticLayout: true,
            readOnly: !this.props.queryEditorEnabled
        };

        let editor;
        if (this.state.showCodeGenQuery) {
            editor = (
                <MonacoEditorControl
                    name="codegenqueryeditor"
                    height="100%"
                    fontSize="13px"
                    language="sql"
                    theme={theme}
                    value={this.state.codeGenQuery}
                    options={options}
                    onChange={() => {return;}}
                    editorWillMount={monaco => this.editorWillMount(monaco)}
                    editorDidMount={editor => this.editorDidMount(editor)}
                />
            );
        } else {
            editor = (
                <MonacoEditorControl
                    name="queryeditor"
                    height="100%"
                    fontSize="13px"
                    language="sql"
                    theme={theme}
                    value={this.props.query}
                    options={options}
                    onChange={query => this.props.onUpdateQuery(query)}
                    editorWillMount={monaco => this.editorWillMount(monaco)}
                    editorDidMount={editor => this.editorDidMount(editor)}
                />
            );
        }

        let src = {};
        if (this.props.errorMessage !== undefined) {
            try {
                let errorMessage = JSON.parse(this.props.errorMessage);
                src = errorMessage['Error'];
            } catch (err) {
                src = this.props.errorMessage;
            }
        } else if (this.state.testQuery !== undefined && this.state.testQuery !== '') {
            try {
                let testResult = JSON.parse(this.state.testQuery);
                try {
                    src = testResult['error'] || testResult.map(t => JSON.parse(t));
                } catch (err) {
                    src = [];
                    JSON.parse(this.state.testQuery).forEach(line => {
                        src.push(line);
                    });
                }
            } catch (err) {
                src = this.state.testQuery;
            }
        }

        const hasKernelId = true;

        let mainEditor;
        if (this.state.fetchingCodeGenQuery) {
            mainEditor = <LoadingPanel showImmediately={true} message="Generating code..." />;
        } else {
            mainEditor = (
                <div style={contentStyle}>
                    <div style={editorHeaderStyle}>
                        <Label className="ms-font-m" style={headerStyle}>
                            Processing Query Script
                        </Label>
                        <div style={rightSideSettingsStyle}>
                            {this.renderCodeEditorThemeButton()}
                            {this.renderPreviewCodeGenQueryButton()}
                            {hasKernelId && this.renderExecuteQueryButton()}
                            {hasKernelId && this.renderResampleButton()}

                            <a style={linkStyle} href={queryExampleWiki} target="_blank" rel="noopener noreferrer">
                                View Example
                            </a>
                        </div>
                    </div>
                    <div style={jsonEditorContainerStyle}>{editor}</div>
                </div>
            );
        }

        let secondEditor;
        if (this.props.isTestQueryOutputPanelVisible && this.props.outputSideToolBarEnabled) {
            if (this.props.fetchingKernel) {
                secondEditor = <LoadingPanel showImmediately={true} message="Initializing interactive experience..." />;
            } else if (this.state.fetchingTestQuery) {
                secondEditor = <LoadingPanel showImmediately={true} message="Executing code..." />;
            } else {
                secondEditor = (
                    <div style={contentStyle}>
                        <div style={editorHeaderStyle}>
                            <Icon
                                style={iconStyle}
                                onClick={() => this.onCloseTestQuery()}
                                iconName="DrillDownSolid"
                                className="ms-IconExample .ms-IconExample:hover"
                            />
                            <Label className="ms-font-m" style={headerStyle}>
                                Test Query Result
                            </Label>
                            <div style={rightSideSettingsStyle}>
                                {hasKernelId && this.renderRefreshKernelsButton()}
                                {this.renderDeleteAllKernelsButton()}
                            </div>
                        </div>
                        <div style={jsonEditorContainerStyle} className="editor-container">
                            <JsonEditor ace={brace} mode="code" theme={themeJsonEditor} allowedModes={['code', 'tree']} value={src} />
                        </div>
                    </div>
                );
            }
        }

        const sideToolBar = (
            <SideToolBar
                isTestQueryOutputPanelVisible={this.props.isTestQueryOutputPanelVisible}
                onShowTestQueryOutputPanel={() => this.props.onShowTestQueryOutputPanel(true)}
            />
        );

        return (
            <div style={containerContentStyle}>
                <SplitterLayout percentage={true} primaryIndex={0}>
                    {mainEditor}
                    {hasKernelId && this.props.isTestQueryOutputPanelVisible && secondEditor}
                </SplitterLayout>
                {hasKernelId && !this.props.isTestQueryOutputPanelVisible && this.props.outputSideToolBarEnabled && sideToolBar}
            </div>
        );
    }

    renderCodeEditorThemeButton() {
        const display = 'Theme';
        return (
            <div className="query-settings" style={toggleStyle}>
                <DefaultButton
                    key={display}
                    className="query-pane-button"
                    title="Change editor color theme"
                    toggle={true}
                    checked={this.state.useDarkEditorTheme}
                    onClick={() => {
                        this.setState({ useDarkEditorTheme: !this.state.useDarkEditorTheme });
                    }}
                >
                    <i style={IconButtonStyles.neutralStyle} className="ms-Icon ms-Icon--Contrast" />
                    {display}
                </DefaultButton>
            </div>
        );
    }

    renderPreviewCodeGenQueryButton() {
        const display = 'Preview';
        return (
            <div className="query-settings" style={toggleStyle}>
                <DefaultButton
                    key={display}
                    className="query-pane-button"
                    title="Translate user code into Spark SQL"
                    toggle={true}
                    disabled={!this.props.previewQueryButtonEnabled}
                    checked={this.state.showCodeGenQuery}
                    onClick={() => this.onUpdatePreviewCodeGenQuery()}
                >
                    <i
                        style={this.props.previewQueryButtonEnabled ? IconButtonStyles.neutralStyle : IconButtonStyles.disabledStyle}
                        className="ms-Icon ms-Icon--Embed"
                    />
                    {display}
                </DefaultButton>
            </div>
        );
    }

    //LiveQuery Run
    renderExecuteQueryButton() {
        const display = 'Run';
        const enableButton = this.props.executeQueryButtonEnabled && this.props.kernelId !== undefined && this.props.kernelId !== '';

        return (
            <div className="query-settings" style={toggleStyle}>
                <DefaultButton
                    key={display}
                    className="query-pane-button"
                    title="Execute query using the diagnostics kernel"
                    disabled={!enableButton}
                    onClick={() => this.onExecuteQuery()}
                >
                    <i
                        style={enableButton ? IconButtonStyles.greenStyle : IconButtonStyles.disabledStyle}
                        className="ms-Icon ms-Icon--Play"
                    />
                    {display}
                </DefaultButton>
            </div>
        );
    }

    renderResampleButton() {
        const display = 'Resample';
        const enableButton =
            this.props.resampleButtonEnabled &&
            (this.props.errorMessage !== undefined || (this.props.kernelId !== undefined && this.props.kernelId !== ''));

        return (
            <div className="query-settings" style={rightSideSettingsStyle}>
                <div style={toggleStyle}>
                    <DefaultButton
                        key={display}
                        className="query-pane-button"
                        title="Resample data"
                        disabled={!enableButton}
                        onClick={() => this.onResampleInput()}
                    >
                        <i
                            style={enableButton ? IconButtonStyles.neutralStyle : IconButtonStyles.disabledStyle}
                            className="ms-Icon ms-Icon--Processing"
                        />
                        {display}
                    </DefaultButton>
                </div>
                <div style={toggleStyle}>
                    <Label className="ms-font-m">Duration in seconds</Label>
                </div>
                <div style={toggleStyle}>
                    <TextField
                        className="query-pane-TextField ms-font-m"
                        spellCheck={false}
                        value={this.props.resamplingInputDuration}
                        onChange={(event, value) => this.props.onUpdateResamplingInputDuration(value)}
                        onGetErrorMessage={value => this.validateNumber(value)}
                    />
                </div>
            </div>
        );
    }

    renderRefreshKernelsButton() {
        const enableButton =
            this.props.refreshKernelsButtonEnabled &&
            (this.props.errorMessage !== undefined || (this.props.kernelId !== undefined && this.props.kernelId !== ''));
        const display = 'Refresh';
        return (
            <div style={toggleStyle}>
                <DefaultButton
                    key={display}
                    className="query-pane-button"
                    title="Refresh diagnostics kernel"
                    disabled={!enableButton}
                    onClick={() => this.onRefreshKernel()}
                >
                    <i
                        style={enableButton ? IconButtonStyles.neutralStyle : IconButtonStyles.disabledStyle}
                        className="ms-Icon ms-Icon--Refresh"
                    />
                    {display}
                </DefaultButton>
            </div>
        );
    }

    renderDeleteAllKernelsButton() {
        const enableButton = this.props.deleteAllKernelsEnabled;
        const display = 'Clear All Kernels';
        return (
            <div style={toggleStyle}>
                <DefaultButton
                    key={display}
                    className="query-pane-button"
                    title="Clear All Kernels"
                    disabled={!enableButton}
                    onClick={() => this.props.onDeleteAllKernels(Actions.updateErrorMessage)}
                >
                    <i
                        style={enableButton ? IconButtonStyles.neutralStyle : IconButtonStyles.disabledStyle}
                        className="ms-Icon ms-Icon--Delete"
                    />
                    {display}
                </DefaultButton>
            </div>
        );
    }

    renderCloseTestQueryButton() {
        return (
            <div style={toggleStyle}>
                <DefaultButton
                    key="refreshKernel"
                    className="query-pane-button"
                    title="refreshKernel"
                    onClick={() => this.onCloseTestQuery()}
                >
                    <i style={IconButtonStyles.greenStyle} className="ms-Icon ms-Icon--ChevronLeft" />
                    Close
                </DefaultButton>
            </div>
        );
    }

    renderDebugModeButton() {
        return (
            <div style={toggleStyle}>
                <DefaultButton
                    key="debugMode"
                    className="query-pane-button"
                    onClick={() => this.toggleDebugMode()}
                    iconProps={{ iconName: 'TestAutoSolid' }}
                />
            </div>
        );
    }

    toggleDebugMode() {
        this.setState({ debugMode: !this.state.debugMode });
    }

    onUpdateCodeEditorTheme(value) {
        this.setState({ useDarkEditorTheme: value });
    }

    onUpdatePreviewCodeGenQuery() {
        const showCodeGenQuery = !this.state.showCodeGenQuery;
        this.setState({ showCodeGenQuery: showCodeGenQuery });

        if (showCodeGenQuery) {
            this.setState({ fetchingCodeGenQuery: true });

            this.props.onGetCodeGenQuery().then(query => {
                this.setState({ codeGenQuery: query });
                this.setState({ fetchingCodeGenQuery: false });
            });
        }
    }

    onRefreshKernel() {
        this.props.onRefreshKernel(this.props.kernelId);
    }

    onCloseTestQuery() {
        this.props.onShowTestQueryOutputPanel(false);
    }

    onExecuteQuery() {
        const selectedQuery = this.getSelectedQuery();
        this.setState({ fetchingTestQuery: true });

        this.props
            .onExecuteQuery(selectedQuery, this.props.kernelId)
            .then(value => {
                this.setState({ testQuery: value });
                this.setState({ fetchingTestQuery: false });
            })
            .catch(error => {
                this.setState({ testQuery: error.message });
                this.setState({ fetchingTestQuery: false });
            });

        this.props.onShowTestQueryOutputPanel(true);
    }

    onResampleInput() {
        this.props.onResampleInput(this.props.kernelId);
    }

    getSelectedQuery() {
        const editor = this.editor.getModel();
        const selection = this.editor.getSelection();

        let selectedText = undefined;
        if (editor && selection && !selection.isEmpty()) {
            selectedText = editor.getValueInRange(selection);
        }

        return selectedText;
    }

    validateNumber(value) {
        return !Helpers.isValidNumberAboveZero(value) ? 'Numbers only and must be greater than zero' : '';
    }
}

// Props
QuerySettingsContent.propTypes = {
    errorMessage: PropTypes.string,
    query: PropTypes.string.isRequired,
    resamplingInputDuration: PropTypes.string.isRequired,
    fetchingKernel: PropTypes.bool.isRequired,
    onUpdateQuery: PropTypes.func.isRequired,
    onGetCodeGenQuery: PropTypes.func.isRequired,
    onRefreshKernel: PropTypes.func.isRequired,
    onDeleteAllKernels: PropTypes.func.isRequired,
    onExecuteQuery: PropTypes.func.isRequired,
    onResampleInput: PropTypes.func.isRequired,
    onUpdateResamplingInputDuration: PropTypes.func.isRequired,
    onGetTableSchemas: PropTypes.func.isRequired,
    outputSideToolBarEnabled: PropTypes.bool.isRequired,
    executeQueryButtonEnabled: PropTypes.bool.isRequired,
    resampleButtonEnabled: PropTypes.bool.isRequired,
    refreshKernelsButtonEnabled: PropTypes.bool.isRequired,
    deleteAllKernelsEnabled: PropTypes.bool.isRequired,
    previewQueryButtonEnabled: PropTypes.bool.isRequired,
    queryEditorEnabled: PropTypes.bool.isRequired
};

// Styles
const rootStyle = {
    display: 'flex',
    flexDirection: 'column',
    overflowY: 'hidden',
    flex: 1,
    height: '100%'
};

const containerContentStyle = {
    flex: 1,
    display: 'flex',
    height: '100%'
};

const iconStyle = {
    transform: 'rotate(270deg)',
    height: '10px',
    width: '10px',
    paddingRight: '12px'
};

const jsonEditorContainerStyle = {
    flex: 1,
    height: '100%',
    overflow: 'hidden',
    display: 'flex',
    flexDirection: 'column'
};

const contentStyle = {
    display: 'flex',
    flexDirection: 'column',
    flex: 1,
    paddingLeft: 10,
    paddingRight: 10,
    paddingTop: 10,
    paddingBottom: 20,
    height: '98%'
};

const headerStyle = {
    display: 'inline-block',
    paddingLeft: 10,
    paddingBottom: 10,
    marginTop: -7
};

const editorHeaderStyle = {
    paddingBottom: 4
};

const rightSideSettingsStyle = {
    display: 'flex',
    flexDirection: 'row',
    height: 27
};

const linkStyle = {
    paddingLeft: 20,
    fontSize: 14,
    lineHeight: '27px',
    color: Colors.themePrimary
};

const toggleStyle = {
    paddingLeft: 5
};
