// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import { connect } from 'react-redux';
import { withRouter } from 'react-router';
import Q from 'q';
import * as Api from '../api';
import * as Helpers from '../flowHelpers';
import * as Models from '../flowModels';
import * as Actions from '../flowActions';
import * as Selectors from '../flowSelectors';
import { DefaultButton, PrimaryButton, Spinner, SpinnerSize, MessageBar, MessageBarType } from 'office-ui-fabric-react';
import { Dialog, DialogType, DialogFooter } from 'office-ui-fabric-react/lib/Dialog';
import { Modal } from 'office-ui-fabric-react/lib/Modal';
import InfoSettingsContent from './info/infoSettingsContent';
import InputSettingsContent from './input/inputSettingsContent';
import ReferenceDataSettingsContent from './referenceData/referenceDataSettingsContent';
import FunctionSettingsContent from './function/functionSettingsContent';
import ScaleSettingsContent from './scale/scaleSettingsContent';
import OutputSettingsContent from './output/outputSettingsContent';
import ScheduleSettingsContent from './schedule/ScheduleSettingsContent';
import RulesSettingsContent from './rule/rulesSettingsContent';
import { functionEnabled } from '../../../common/api';
import {
    Colors,
    Panel,
    PanelHeader,
    PanelHeaderButtons,
    LoadingPanel,
    IconButtonStyles,
    VerticalTabs,
    VerticalTabItem,
    getApiErrorMessage
} from 'datax-common';
import {
    QueryApi,
    KernelActions,
    KernelSelectors,
    QueryActions,
    LayoutActions,
    LayoutSelectors,
    QuerySelectors,
    QuerySettingsContent
} from 'datax-query';

class FlowDefinitionPanel extends React.Component {
    constructor(props) {
        super(props);

        this.state = {
            showMessageBar: false,
            messageBarIsError: false,
            messageBarStatement: undefined,
            showConfirmationDialog: false,
            confirmationMessage: undefined,
            confirmationHandler: undefined,
            loading: true,
            isSaving: false,
            isDeleting: false,
            timer: null,
            counter: 0,
            inProgessDialogMessage: '',
            havePermission: true,
            saveFlowButtonEnabled: false,
            deployFlowButtonEnabled: false,
            deleteFlowButtonEnabled: false,
            getInputSchemaButtonEnabled: false,
            outputSideToolBarEnabled: false,
            executeQueryButtonEnabled: false,
            resampleButtonEnabled: false,
            refreshKernelsButtonEnabled: false,
            deleteAllKernelsEnabled: false,
            flowNameTextboxEnabled: false,
            inputModeDropdownEnabled: false,
            inputTypeDropdownEnabled: false,
            inputEventHubEnabled: false,
            inputEventHubConnectionStringEnabled: false,
            inputWindowDurationTextboxEnabled: false,
            inputMaxRateTextboxEnabled: false,
            inputTimestampColumnEnabled: false,
            inputWatermarkEnabled: false,
            inputSchemaEditorEnabled: false,
            inputNormalizationEditorEnabled: false,
            addRuleButtonEnabled: false,
            deleteRuleButtonEnabled: false,
            previewQueryButtonEnabled: false,
            queryEditorEnabled: false,
            addReferenceDataButtonEnabled: false,
            deleteReferenceDataButtonEnabled: false,
            addFunctionButtonEnabled: false,
            deleteFunctionButtonEnabled: false,
            addOutputSinkButtonEnabled: false,
            deleteOutputSinkButtonEnabled: false,
            scaleNumExecutorsSliderEnabled: false,
            scaleExecutorMemorySliderEnabled: false,

            addBatchButtonEnabled: false,
            deleteBatchButtonEnabled: false
        };

        this.handleWindowClose = this.handleWindowClose.bind(this);
    }

    tick() {
        this.setState({
            counter: this.state.counter + 1
        });
    }

    handleWindowClose() {
        QueryApi.deleteDiagnosticKernelOnUnload(this.props.kernelId);
        this.sleep(500);
    }

    componentWillUnmount() {
        window.removeEventListener('beforeunload', this.handleWindowClose);
        clearInterval(this.state.timer);

        if (this.props.kernelId !== undefined && this.props.kernelId !== '') {
            this.onDeleteKernel(this.props.kernelId, this.props.flow.name);
        }
    }

    sleep(delay) {
        let start = new Date().getTime();
        while (new Date().getTime() < start + delay);
    }

    componentDidMount() {
        window.addEventListener('beforeunload', this.handleWindowClose);

        // Initialize onebox mode flag so that the UI can show different defaults for local vs cloud mode
        Api.initOneBoxMode()
            .then(result => {
                this.props.onUpdateOneBoxMode(result.enableLocalOneBox);
                return;
            })
            .then(() => {
                Q(
                    this.props.initFlow({
                        id: this.props.match.params.id
                    })
                ).then(() => {
                    this.setState({ loading: false });
                    this.props.onUpdateWarningMessage(undefined);
                    this.getKernel();
                });
            })
            .catch(error => {
                this.setState({
                    loading: false,
                    showMessageBar: true,
                    messageBarIsError: true,
                    messageBarStatement: getApiErrorMessage(error),
                    havePermission: false
                });
            });

        functionEnabled().then(response => {
            this.setState({
                saveFlowButtonEnabled: response.saveFlowButtonEnabled ? true : false,
                deployFlowButtonEnabled: response.deployFlowButtonEnabled ? true : false,
                deleteFlowButtonEnabled: response.deleteFlowButtonEnabled ? true : false,
                getInputSchemaButtonEnabled: response.getInputSchemaButtonEnabled ? true : false,
                outputSideToolBarEnabled: response.outputSideToolBarEnabled ? true : false,
                executeQueryButtonEnabled: response.executeQueryButtonEnabled ? true : false,
                resampleButtonEnabled: response.resampleButtonEnabled ? true : false,
                refreshKernelsButtonEnabled: response.refreshKernelsButtonEnabled ? true : false,
                deleteAllKernelsEnabled: response.deleteAllKernelsEnabled ? true : false,
                flowNameTextboxEnabled: response.flowNameTextboxEnabled ? true : false,
                inputModeDropdownEnabled: response.inputModeDropdownEnabled ? true : false,
                inputTypeDropdownEnabled: response.inputTypeDropdownEnabled ? true : false,
                inputEventHubEnabled: response.inputEventHubEnabled ? true : false,
                inputEventHubConnectionStringEnabled: response.inputEventHubConnectionStringEnabled ? true : false,
                inputWindowDurationTextboxEnabled: response.inputWindowDurationTextboxEnabled ? true : false,
                inputMaxRateTextboxEnabled: response.inputMaxRateTextboxEnabled ? true : false,
                inputTimestampColumnEnabled: response.inputTimestampColumnEnabled ? true : false,
                inputWatermarkEnabled: response.inputWatermarkEnabled ? true : false,
                inputSchemaEditorEnabled: response.inputSchemaEditorEnabled ? true : false,
                inputNormalizationEditorEnabled: response.inputNormalizationEditorEnabled ? true : false,
                addRuleButtonEnabled: response.addRuleButtonEnabled ? true : false,
                deleteRuleButtonEnabled: response.deleteRuleButtonEnabled ? true : false,
                previewQueryButtonEnabled: response.previewQueryButtonEnabled ? true : false,
                queryEditorEnabled: response.queryEditorEnabled ? true : false,
                addReferenceDataButtonEnabled: response.addReferenceDataButtonEnabled ? true : false,
                deleteReferenceDataButtonEnabled: response.deleteReferenceDataButtonEnabled ? true : false,
                addFunctionButtonEnabled: response.addFunctionButtonEnabled ? true : false,
                deleteFunctionButtonEnabled: response.deleteFunctionButtonEnabled ? true : false,
                addOutputSinkButtonEnabled: response.addOutputSinkButtonEnabled ? true : false,
                deleteOutputSinkButtonEnabled: response.deleteOutputSinkButtonEnabled ? true : false,
                scaleNumExecutorsSliderEnabled: response.scaleNumExecutorsSliderEnabled ? true : false,
                scaleExecutorMemorySliderEnabled: response.scaleExecutorMemorySliderEnabled ? true : false,

                addBatchButtonEnabled: response.addBatchButtonEnabled ? true : false,
                deleteBatchButtonEnabled: response.deleteBatchButtonEnabled ? true : false
            });
        });
    }

    render() {
        return (
            <Panel>
                <PanelHeader>Flow Definition</PanelHeader>
                <PanelHeaderButtons>{this.renderButtons()}</PanelHeaderButtons>

                {this.renderMessageBar()}
                {this.renderDialog()}
                {this.renderModal()}

                {this.renderContent()}
            </Panel>
        );
    }

    renderButtons() {
        if (this.state.havePermission) {
            return [
                this.renderBackButton(),
                this.renderSaveButton(),
                this.renderDeployButton(),
                this.renderDeleteButton(),
                this.renderDebugButton()
            ];
        }
    }

    renderBackButton() {
        const buttonTooltip = this.props.flow.isDirty || this.props.isQueryDirty ? 'Discard Changes' : 'Go Back';
        let buttonText = this.props.flow.isDirty || this.props.isQueryDirty ? 'Cancel' : 'Back';
        let buttonIcon = <i style={IconButtonStyles.neutralStyle} className="ms-Icon ms-Icon--NavigateBack" />;

        if (this.state.loading) {
            // render the button as empty during load but keep it existing so that button toolbar is visible
            buttonText = '';
            buttonIcon = null;
        }

        return (
            <DefaultButton key="back" className="header-button" title={buttonTooltip} onClick={() => this.onCancel()}>
                {buttonIcon}
                {buttonText}
            </DefaultButton>
        );
    }

    renderSaveButton() {
        if (!this.state.loading) {
            //Will be enabled when user has access and flow has been changed. 
            const enableButton = (this.props.flow.isDirty || this.props.isQueryDirty) && this.state.saveFlowButtonEnabled;
            return (
                <DefaultButton
                    key="save"
                    className="header-button"
                    disabled={!enableButton}
                    title="Save the Flow"
                    onClick={() => this.onSaveDefinition()}
                >
                    <i
                        style={enableButton ? IconButtonStyles.greenStyle : IconButtonStyles.disabledStyle}
                        className="ms-Icon ms-Icon--Save"
                    />
                    Save
                </DefaultButton>
            );
        } else {
            return null;
        }
    }

    renderDeployButton() {
        if (!this.state.loading) {
            const enableButton =
                //Will be enabled when user has access and flow has valid configuration. 
                this.props.flowValidated && this.state.deployFlowButtonEnabled;
            return (
                <DefaultButton
                    key="deploy"
                    className="header-button"
                    disabled={!enableButton}
                    title="Save, deploy and restart the Flow job"
                    onClick={() => this.onDeployDefinition()}
                >
                    <i
                        style={enableButton ? IconButtonStyles.greenStyle : IconButtonStyles.disabledStyle}
                        className="ms-Icon ms-Icon--Deploy"
                    />
                    Deploy
                </DefaultButton>
            );
        } else {
            return null;
        }
    }

    renderDeleteButton() {
        const supportDelete = true;
        const enableButton = supportDelete && this.state.deleteFlowButtonEnabled;
        if (supportDelete && !this.state.loading && !this.props.flow.isNew) {
            return (
                <DefaultButton
                    key="delete"
                    className="header-button"
                    disabled={!enableButton}
                    title="Delete Flow definition"
                    onClick={() => this.onDeleteDefinition()}
                >
                    <i
                        style={enableButton ? IconButtonStyles.neutralStyle : IconButtonStyles.disabledStyle}
                        className="ms-Icon ms-Icon--Delete"
                    />
                    Delete
                </DefaultButton>
            );
        } else {
            return null;
        }
    }

    renderDebugButton() {
        // Whether or not to show a debug button to dump the flow object into the browser's console window
        const showDebugButton = false;
        if (showDebugButton && !this.state.loading) {
            return (
                <DefaultButton key="debug" className="header-button" onClick={() => this.onDebugClick()}>
                    <i style={IconButtonStyles.neutralStyle} className="ms-Icon ms-Icon--Bug" />
                    Debug
                </DefaultButton>
            );
        } else {
            return null;
        }
    }

    renderContent() {
        if (this.state.havePermission) {
            if (this.state.loading) {
                return this.renderFlowLoading();
            } else {
                return this.renderFlow();
            }
        }
    }

    renderFlowLoading() {
        // To avoid jarring flashing UI, we minimally display an empty tabs container with a loading spinner
        return (
            <div style={contentStyle}>
                <div style={tabContainerStyle}>
                    <VerticalTabs>
                        <VerticalTabItem linkText="Info" itemCompleted={() => true}>
                            <LoadingPanel showImmediately={true} />
                        </VerticalTabItem>
                    </VerticalTabs>
                </div>
            </div>
        );
    }

    renderScheduleTab() {
        if (this.props.input.mode === Models.inputModeEnum.streaming) {
            return null;
        } else {
            return (
                <VerticalTabItem linkText="Schedule" itemCompleted={this.props.scheduleValidated}>
                    <ScheduleSettingsContent
                        batchList={this.props.flow.batchList}
                        selectedBatchIndex={this.props.selectedBatchIndex}
                        onNewBatch={this.props.onNewBatch}
                        onDeleteBatch={this.props.onDeleteBatch}
                        onUpdateSelectedBatchIndex={this.props.onUpdateSelectedBatchIndex}
                        addBatchButtonEnabled={this.state.addBatchButtonEnabled}
                        deleteBatchButtonEnabled={this.state.deleteBatchButtonEnabled}
                        onUpdateBatchName={this.props.onUpdateBatchName}
                        onUpdateBatchStartTime={this.props.onUpdateBatchStartTime}
                        onUpdateBatchEndTime={this.props.onUpdateBatchEndTime}
                        onUpdateBatchIntervalValue={this.props.onUpdateBatchIntervalValue}
                        onUpdateBatchIntervalType={this.props.onUpdateBatchIntervalType}
                        onUpdateBatchDelayValue={this.props.onUpdateBatchDelayValue}
                        onUpdateBatchDelayType={this.props.onUpdateBatchDelayType}
                        onUpdateBatchWindowValue={this.props.onUpdateBatchWindowValue}
                        onUpdateBatchWindowType={this.props.onUpdateBatchWindowType}
                    />
                </VerticalTabItem>
            );
        }
    }

    renderFlow() {
        return (
            <div style={contentStyle}>
                <div style={tabContainerStyle}>
                    <VerticalTabs>
                        <VerticalTabItem linkText="Info" itemCompleted={this.props.infoValidated}>
                            <InfoSettingsContent
                                displayName={this.props.flow.displayName}
                                name={this.props.flow.name}
                                owner={this.props.flow.owner}
                                databricksToken={this.props.flow.databricksToken}
                                onUpdateDisplayName={this.props.onUpdateDisplayName}
                                onUpdateDatabricksToken={this.props.onUpdateDatabricksToken}
                                flowNameTextboxEnabled={this.state.flowNameTextboxEnabled}
                                isDatabricksSparkType={this.props.flow.isDatabricksSparkType}
                                saveFlowButtonEnabled={this.state.saveFlowButtonEnabled}
                                saveFlowAndInitializeKernel={() => this.saveFlowAndInitializeKernel()}
                            />
                        </VerticalTabItem>

                        <VerticalTabItem linkText="Input" itemCompleted={this.props.inputValidated}>
                            <InputSettingsContent
                                input={this.props.flow.input}
                                batchInputs={this.props.flow.batchInputs}
                                selectedFlowBatchInputIndex={this.props.flow.selectedFlowBatchInputIndex}
                                fetchingInputSchema={this.props.flow.fetchingInputSchema}
                                samplingInputDuration={this.props.flow.samplingInputDuration}
                                timer={this.state.counter}
                                enableLocalOneBox={this.props.enableLocalOneBox}
                                onUpdateMode={this.props.onUpdateInputMode}
                                onUpdateType={this.props.onUpdateInputType}
                                onUpdateHubName={this.props.onUpdateInputHubName}
                                onUpdateBatchInputPath={this.props.onUpdateBatchInputPath}
                                onUpdateBatchInputConnection={this.props.onUpdateBatchInputConnection}
                                onUpdateBatchInputFormatType={this.props.onUpdateBatchInputFormatType}
                                onUpdateBatchInputCompressionType={this.props.onUpdateBatchInputCompressionType}
                                onUpdateHubConnection={this.props.onUpdateInputHubConnection}
                                onUpdateSubscriptionId={this.props.onUpdateInputSubscriptionId}
                                onUpdateResourceGroup={this.props.onUpdateInputResourceGroup}
                                onUpdateJobMode={this.props.onUpdateJobMode}
                                onUpdateRecurrence={this.props.onUpdateRecurrence}
                                onUpdateStartTime={this.props.onUpdateStartTime}
                                onUpdateEndTime={this.props.onUpdateEndTime}
                                onUpdateOffset={this.props.onUpdateOffset}
                                onUpdateWindowDuration={this.props.onUpdateInputWindowDuration}
                                onUpdateTimestampColumn={this.props.onUpdateInputTimestampColumn}
                                onUpdateWatermarkValue={this.props.onUpdateInputWatermarkValue}
                                onUpdateWatermarkUnit={this.props.onUpdateInputWatermarkUnit}
                                onUpdateMaxRate={this.props.onUpdateInputMaxRate}
                                onUpdateSchema={this.props.onUpdateInputSchema}
                                onGetInputSchema={() => this.onGetInputSchema()}
                                onUpdateShowNormalizationSnippet={this.props.onUpdateShowNormalizationSnippet}
                                onUpdateNormalizationSnippet={this.props.onUpdateNormalizationSnippet}
                                onUpdateSamplingInputDuration={this.props.onUpdateSamplingInputDuration}
                                getInputSchemaButtonEnabled={this.state.getInputSchemaButtonEnabled}
                                inputModeDropdownEnabled={this.state.inputModeDropdownEnabled}
                                inputTypeDropdownEnabled={this.state.inputTypeDropdownEnabled}
                                inputEventHubEnabled={this.state.inputEventHubEnabled}
                                inputEventHubConnectionStringEnabled={this.state.inputEventHubConnectionStringEnabled}
                                inputWindowDurationTextboxEnabled={this.state.inputWindowDurationTextboxEnabled}
                                inputMaxRateTextboxEnabled={this.state.inputMaxRateTextboxEnabled}
                                inputTimestampColumnEnabled={this.state.inputTimestampColumnEnabled}
                                inputWatermarkEnabled={this.state.inputWatermarkEnabled}
                                inputSchemaEditorEnabled={this.state.inputSchemaEditorEnabled}
                                inputNormalizationEditorEnabled={this.state.inputNormalizationEditorEnabled}
                            />
                        </VerticalTabItem>

                        <VerticalTabItem linkText="Rules" itemCompleted={this.props.rulesValidated}>
                            <RulesSettingsContent
                                ruleItems={this.props.rules}
                                selectedRuleIndex={this.props.selectedRuleIndex}
                                sinkers={this.props.outputs}
                                outputTemplates={this.props.outputTemplates}
                                onNewRule={this.props.onNewRule}
                                onDeleteRule={this.props.onDeleteRule}
                                onUpdateSelectedRuleIndex={this.props.onUpdateSelectedRuleIndex}
                                onUpdateRuleName={this.props.onUpdateRuleName}
                                onUpdateTagRuleSubType={this.props.onUpdateTagRuleSubType}
                                onUpdateTagRuleDescription={this.props.onUpdateTagRuleDescription}
                                onUpdateTagTag={this.props.onUpdateTagTag}
                                onUpdateTagIsAlert={this.props.onUpdateTagIsAlert}
                                onUpdateTagSinks={this.props.onUpdateTagSinks}
                                onUpdateTagSeverity={this.props.onUpdateTagSeverity}
                                onUpdateTagConditions={this.props.onUpdateTagConditions}
                                onUpdateTagAggregates={this.props.onUpdateTagAggregates}
                                onUpdateTagPivots={this.props.onUpdateTagPivots}
                                onUpdateSchemaTableName={this.props.onUpdateSchemaTableName}
                                onGetTableSchemas={() => this.onGetTableSchemas()}
                                addRuleButtonEnabled={this.state.addRuleButtonEnabled}
                                deleteRuleButtonEnabled={this.state.deleteRuleButtonEnabled}
                            />
                        </VerticalTabItem>

                        <VerticalTabItem linkText="Query" itemCompleted={this.props.queryValidated}>
                            <QuerySettingsContent
                                errorMessage={this.props.errorMessage}
                                query={this.props.query}
                                kernelId={this.props.kernelId}
                                fetchingKernel={this.props.fetchingKernel}
                                resamplingInputDuration={this.props.flow.resamplingInputDuration}
                                onUpdateQuery={this.props.onUpdateQuery}
                                onGetCodeGenQuery={() => this.onGetCodeGenQuery()}
                                onRefreshKernel={kernelId => this.refreshKernel(kernelId)}
                                onDeleteAllKernels={() => this.deleteAllKernel()}
                                onExecuteQuery={(selectedQuery, kernelId) => this.onExecuteQuery(selectedQuery, kernelId)}
                                onResampleInput={kernelId => this.onResampleInput(kernelId)}
                                onShowTestQueryOutputPanel={isVisible => this.onShowTestQueryOutputPanel(isVisible)}
                                onUpdateResamplingInputDuration={this.props.onUpdateResamplingInputDuration}
                                onGetTableSchemas={() => this.onGetTableSchemas()}
                                isTestQueryOutputPanelVisible={this.props.isTestQueryOutputPanelVisible}
                                outputSideToolBarEnabled={this.state.outputSideToolBarEnabled}
                                executeQueryButtonEnabled={this.state.executeQueryButtonEnabled}
                                resampleButtonEnabled={this.state.resampleButtonEnabled}
                                refreshKernelsButtonEnabled={this.state.refreshKernelsButtonEnabled}
                                deleteAllKernelsEnabled={this.state.deleteAllKernelsEnabled}
                                previewQueryButtonEnabled={this.state.previewQueryButtonEnabled}
                                queryEditorEnabled={this.state.queryEditorEnabled}
                            />
                        </VerticalTabItem>

                        <VerticalTabItem linkText="Reference Data" itemCompleted={this.props.referenceDataValidated}>
                            <ReferenceDataSettingsContent
                                referenceDataItems={this.props.referenceData}
                                selectedReferenceDataIndex={this.props.selectedReferenceDataIndex}
                                enableLocalOneBox={this.props.enableLocalOneBox}
                                onNewReferenceData={this.props.onNewReferenceData}
                                onDeleteReferenceData={this.props.onDeleteReferenceData}
                                onUpdateSelectedReferenceDataIndex={this.props.onUpdateSelectedReferenceDataIndex}
                                onUpdateReferenceDataName={this.props.onUpdateReferenceDataName}
                                onUpdateCsvPath={this.props.onUpdateCsvPath}
                                onUpdateCsvDelimiter={this.props.onUpdateCsvDelimiter}
                                onUpdateCsvContainsHeader={this.props.onUpdateCsvContainsHeader}
                                addReferenceDataButtonEnabled={this.state.addReferenceDataButtonEnabled}
                                deleteReferenceDataButtonEnabled={this.state.deleteReferenceDataButtonEnabled}
                            />
                        </VerticalTabItem>

                        <VerticalTabItem linkText="Functions" itemCompleted={this.props.functionsValidated}>
                            <FunctionSettingsContent
                                functionItems={this.props.functions}
                                selectedFunctionIndex={this.props.selectedFunctionIndex}
                                enableLocalOneBox={this.props.enableLocalOneBox}
                                onNewFunction={this.props.onNewFunction}
                                onDeleteFunction={this.props.onDeleteFunction}
                                onUpdateSelectedFunctionIndex={this.props.onUpdateSelectedFunctionIndex}
                                onUpdateFunctionName={this.props.onUpdateFunctionName}
                                onUpdateUdfPath={this.props.onUpdateUdfPath}
                                onUpdateUdfClass={this.props.onUpdateUdfClass}
                                onUpdateUdfDependencyLibs={this.props.onUpdateUdfDependencyLibs}
                                onUpdateAzureFunctionServiceEndpoint={this.props.onUpdateAzureFunctionServiceEndpoint}
                                onUpdateAzureFunctionApi={this.props.onUpdateAzureFunctionApi}
                                onUpdateAzureFunctionCode={this.props.onUpdateAzureFunctionCode}
                                onUpdateAzureFunctionMethodType={this.props.onUpdateAzureFunctionMethodType}
                                onUpdateAzureFunctionParams={this.props.onUpdateAzureFunctionParams}
                                addFunctionButtonEnabled={this.state.addFunctionButtonEnabled}
                                deleteFunctionButtonEnabled={this.state.deleteFunctionButtonEnabled}
                            />
                        </VerticalTabItem>

                        <VerticalTabItem linkText="Outputs" itemCompleted={this.props.outputsValidated}>
                            <OutputSettingsContent
                                flowId={this.props.flow.name}
                                sinkerItems={this.props.outputs}
                                selectedSinkerIndex={this.props.selectedSinkerIndex}
                                enableLocalOneBox={this.props.enableLocalOneBox}
                                onNewSinker={this.props.onNewSinker}
                                onDeleteSinker={this.props.onDeleteSinker}
                                onUpdateSelectedSinkerIndex={this.props.onUpdateSelectedSinkerIndex}
                                onUpdateSinkerName={this.props.onUpdateSinkerName}
                                onUpdateCosmosDbConnection={this.props.onUpdateCosmosDbConnection}
                                onUpdateCosmosDbDatabase={this.props.onUpdateCosmosDbDatabase}
                                onUpdateCosmosDbCollection={this.props.onUpdateCosmosDbCollection}
                                onUpdateEventHubConnection={this.props.onUpdateEventHubConnection}
                                onUpdateBlobConnection={this.props.onUpdateBlobConnection}
                                onUpdateBlobContainerName={this.props.onUpdateBlobContainerName}
                                onUpdateBlobPrefix={this.props.onUpdateBlobPrefix}
                                onUpdateBlobPartitionFormat={this.props.onUpdateBlobPartitionFormat}
                                onUpdateFormatType={this.props.onUpdateFormatType}
                                onUpdateCompressionType={this.props.onUpdateCompressionType}
                                onUpdateSqlConnection={this.props.onUpdateSqlConnection}
                                onUpdateSqlTableName={this.props.onUpdateSqlTableName}
                                onUpdateSqlWriteMode={this.props.onUpdateSqlWriteMode}
                                onUpdateSqlUseBulkInsert={this.props.onUpdateSqlUseBulkInsert}
                                addOutputSinkButtonEnabled={this.state.addOutputSinkButtonEnabled}
                                deleteOutputSinkButtonEnabled={this.state.deleteOutputSinkButtonEnabled}
                            />
                        </VerticalTabItem>

                        <VerticalTabItem linkText="Scale" itemCompleted={this.props.scaleValidated}>
                            <ScaleSettingsContent
                                scale={this.props.scale}
                                onUpdateNumExecutors={this.props.onUpdateNumExecutors}
                                onUpdateExecutorMemory={this.props.onUpdateExecutorMemory}
                                scaleNumExecutorsSliderEnabled={this.state.scaleNumExecutorsSliderEnabled}
                                scaleExecutorMemorySliderEnabled={this.state.scaleExecutorMemorySliderEnabled}
                                isDatabricksSparkType={this.props.flow.isDatabricksSparkType}
                                onUpdateDatabricksAutoScale={this.props.onUpdateDatabricksAutoScale}
                                onUpdateDatabricksMinWorkers={this.props.onUpdateDatabricksMinWorkers}
                                onUpdateDatabricksMaxWorkers={this.props.onUpdateDatabricksMaxWorkers}
                            />
                        </VerticalTabItem>

                        {this.renderScheduleTab()}
                    </VerticalTabs>
                </div>
            </div>
        );
    }

    renderMessageBar() {
        if (this.state.showMessageBar) {
            return (
                <MessageBar
                    messageBarType={this.state.messageBarIsError ? MessageBarType.error : MessageBarType.success}
                    onDismiss={() => this.onDismissMessageBar()}
                >
                    {`${this.state.messageBarIsError ? 'Error' : 'Success'} - ${this.state.messageBarStatement}`}
                </MessageBar>
            );
        } else if (this.props.warningMessage) {
            return (
                <MessageBar messageBarType={MessageBarType.warning} onDismiss={() => this.onDismissMessageBar()}>
                    {this.props.warningMessage}
                </MessageBar>
            );
        } else {
            return null;
        }
    }

    renderDialog() {
        return (
            <Dialog
                hidden={!this.state.showConfirmationDialog}
                title="Delete Flow"
                onDismiss={() => this.onDismissDialog()}
                dialogContentProps={{
                    type: DialogType.largeHeader,
                    subText: this.state.confirmationMessage
                }}
                modalProps={{
                    isBlocking: true,
                    containerClassName: 'ms-dialogMainOverride'
                }}
            >
                <DialogFooter>
                    <PrimaryButton onClick={() => this.state.confirmationHandler()} text="Delete" />
                    <DefaultButton onClick={() => this.onDismissDialog()} text="Cancel" />
                </DialogFooter>
            </Dialog>
        );
    }

    renderModal() {
        return (
            <Modal isOpen={this.state.isSaving || this.state.isDeleting} isBlocking={true} containerClassName="ms-modal-container">
                <div className="ms-font-xxl ms-fontWeight-semibold" style={modelHeaderStyle}>
                    <span>{this.state.inProgessDialogMessage}</span>
                </div>
                <div style={modalBodyStyle}>
                    <Spinner size={SpinnerSize.large} label="This operation may take minutes..." />
                </div>
            </Modal>
        );
    }

    onDismissMessageBar() {
        this.setState({
            showMessageBar: false,
            messageBarIsError: false,
            messageBarStatement: undefined
        });

        this.props.onUpdateWarningMessage(undefined);
    }

    onDismissDialog() {
        this.setState({
            showConfirmationDialog: false,
            confirmationHandler: undefined,
            confirmationMessage: undefined
        });
    }

    onSaveDefinition() {
        this.setState({
            isSaving: true,
            showMessageBar: false,
            messageBarIsError: false,
            inProgessDialogMessage: 'Save in progress'
        });

        this.props
            .onSaveFlow(this.props.flow, this.props.query)
            .then(name => {
                this.setState({
                    isSaving: false,
                    showMessageBar: true,
                    messageBarIsError: false,
                    messageBarStatement:
                        this.props.input.mode === Models.inputModeEnum.streaming
                            ? 'Flow definition is saved'
                            : 'Flow definition is saved. If there are any scheduled batch jobs in this flow, they will be created and started by the scheduler.'
                });

                this.props.initFlow({ id: name });
            })
            .catch(error => {
                this.setState({
                    isSaving: false,
                    showMessageBar: true,
                    messageBarIsError: true,
                    messageBarStatement: getApiErrorMessage(error)
                });
            });
    }

    onDeployDefinition() {
        this.setState({
            isSaving: true,
            showMessageBar: false,
            messageBarIsError: false,
            inProgessDialogMessage: 'Deployment in progress'
        });

        this.props
            .onDeployFlow(this.props.flow, this.props.query)
            .then(name => {
                this.setState({
                    isSaving: false,
                    showMessageBar: true,
                    messageBarIsError: false,
                    messageBarStatement: 'Flow definition is saved and deployed'
                });

                this.props.initFlow({ id: name });
            })
            .catch(error => {
                this.setState({
                    isSaving: false,
                    showMessageBar: true,
                    messageBarIsError: true,
                    messageBarStatement: getApiErrorMessage(error)
                });
            });
    }

    onDeleteDefinition() {
        this.setState({
            showConfirmationDialog: true,
            confirmationHandler: () => this.onDeleteConfirmed(),
            confirmationMessage: 'Are you sure you want to delete this definition?'
        });
    }

    onDeleteConfirmed() {
        this.setState({
            isDeleting: true,
            showConfirmationDialog: false,
            showMessageBar: false,
            messageBarIsError: false,
            inProgessDialogMessage: 'Deletion in progress'
        });
        this.props
            .onDeleteFlow(this.props.flow)
            .then(result => {
                this.setState({
                    isDeleting: false,
                    showConfirmationDialog: false,
                    showMessageBar: true,
                    messageBarIsError: false,
                    messageBarStatement: 'Flow definition is deleted'
                });
                this.props.history.push(this.props.returnPath);
            })
            .catch(error => {
                this.setState({
                    isDeleting: false,
                    showConfirmationDialog: false,
                    showMessageBar: true,
                    messageBarIsError: true,
                    messageBarStatement: getApiErrorMessage(error)
                });
            });
    }

    onCancel() {
        if (this.props.returnPath) {
            this.props.history.push(this.props.returnPath);
        } else {
            this.props.history.goBack();
        }
    }

    onDebugClick() {
        console.log('Flow object');
        console.log(this.props.flow);

        console.log('Config object');
        const config = Helpers.convertFlowToConfig(this.props.flow, this.props.query);
        console.log(config);

        console.log('Flow object from Config');
        const flow = Helpers.convertConfigToFlow(config);
        console.log(flow);
    }

    onShowTestQueryOutputPanel(isVisible) {
        this.props.onShowTestQueryOutputPanel(isVisible);
    }

    onGetInputSchema() {
        this.setState({ counter: 0 });
        clearInterval(this.state.timer);
        let timer = setInterval(this.tick.bind(this), 1000);
        this.setState({ timer });

        return this.props
            .onGetInputSchema(this.props.flow)
            .then(() => {
                if (this.props.kernelId !== undefined && this.props.kernelId !== '') {
                    this.refreshKernel(this.props.kernelId);
                } else {
                    this.getKernel();
                }
            })
            .catch(error => {
                const message = getApiErrorMessage(error);
                return Q.reject({ error: true, message: message });
            });
    }

    onGetTableSchemas() {
        return this.props.onGetTableSchemas(Helpers.convertFlowToQueryMetadata(this.props.flow, this.props.query));
    }

    onGetCodeGenQuery() {
        return this.props.onGetCodeGenQuery(Helpers.convertFlowToQueryMetadata(this.props.flow, this.props.query));
    }

    getKernel() {
        if (
            this.props.input.properties.inputSchemaFile !== Models.defaultInput.properties.inputSchemaFile &&
            this.props.input.properties.inputSchemaFile !== '{}'
        ) {
            const version = this.props.kernelVersion + 1;
            this.props.onUpdateKernelVersion(version);
            this.props.onGetKernel(
                Helpers.convertFlowToQueryMetadata(this.props.flow, this.props.query),
                version,
                QueryActions.updateErrorMessage
            );
        }
    }

    refreshKernel(kernelId) {
        const version = this.props.kernelVersion + 1;
        this.props.onUpdateKernelVersion(version);
        this.props.onRefreshKernel(
            Helpers.convertFlowToQueryMetadata(this.props.flow, this.props.query),
            kernelId,
            version,
            QueryActions.updateErrorMessage
        );
    }

    deleteAllKernel() {
        this.props.onDeleteAllKernels(QueryActions.updateErrorMessage, this.props.flow.name);
    }

    onDeleteKernel(kernelId, flowName) {
        const version = this.props.kernelVersion + 1;
        this.props.onUpdateKernelVersion(version);
        return this.props.onDeleteKernel(kernelId, version, flowName);
    }

    onResampleInput(kernelId) {
        const version = this.props.kernelVersion + 1;
        this.props.onUpdateKernelVersion(version);
        this.props.onResampleInput(Helpers.convertFlowToQueryMetadata(this.props.flow, this.props.query), kernelId, version);
    }

    onExecuteQuery(selectedQuery, kernelId) {
        return this.props.onExecuteQuery(Helpers.convertFlowToQueryMetadata(this.props.flow, this.props.query), selectedQuery, kernelId);
    }

    saveFlowAndInitializeKernel() {
        Q(
            this.onSaveDefinition()
        ).then(() => {
            this.getKernel();
        })
    }
}

// Props
FlowDefinitionPanel.propTypes = {
    returnPath: PropTypes.string
};

// State Props
const mapStateToProps = state => ({
    // Settings
    flow: Selectors.getFlow(state),
    input: Selectors.getFlowInput(state),
    referenceData: Selectors.getFlowReferenceData(state),
    functions: Selectors.getFlowFunctions(state),
    query: QuerySelectors.getQueryContent(state),
    isQueryDirty: QuerySelectors.getQueryDirty(state),
    scale: Selectors.getFlowScale(state),
    outputs: Selectors.getFlowOutputs(state),
    outputTemplates: Selectors.getFlowOutputTemplates(state),
    rules: Selectors.getFlowRules(state),

    // State
    selectedBatchIndex: Selectors.getSelectedBatchIndex(state),

    selectedReferenceDataIndex: Selectors.getSelectedReferenceDataIndex(state),
    selectedFunctionIndex: Selectors.getSelectedFunctionIndex(state),
    selectedSinkerIndex: Selectors.getSelectedSinkerIndex(state),
    selectedOutputTemplateIndex: Selectors.getSelectedOutputTemplateIndex(state),
    selectedRuleIndex: Selectors.getSelectedRuleIndex(state),
    isTestQueryOutputPanelVisible: LayoutSelectors.getTestQueryOutputPanelVisibility(state),
    kernelId: KernelSelectors.getKernelId(state),
    kernelVersion: KernelSelectors.getKernelVersion(state),
    fetchingKernel: KernelSelectors.getFetchingKernel(state),
    errorMessage: Selectors.getErrorMessage(state),
    warningMessage: Selectors.getWarningMessage(state),
    enableLocalOneBox: Selectors.getEnableLocalOneBox(state),

    // Validation
    infoValidated: Selectors.validateFlowInfo(state),
    inputValidated: Selectors.validateFlowInput(state),
    referenceDataValidated: Selectors.validateFlowReferenceData(state),
    functionsValidated: Selectors.validateFlowFunctions(state),
    scaleValidated: Selectors.validateFlowScale(state),
    outputsValidated: Selectors.validateFlowOutputs(state),
    rulesValidated: Selectors.validateFlowRules(state),
    flowValidated: Selectors.validateFlow(state),
    queryValidated: QuerySelectors.validateQueryTab(state),
    scheduleValidated: Selectors.validateFlowSchedule(state)
});

// Dispatch Props
const mapDispatchToProps = dispatch => ({
    // Init Actions
    initFlow: context => dispatch(Actions.initFlow(context)),
    // Message Actions
    onUpdateWarningMessage: message => dispatch(Actions.updateWarningMessage(message)),

    // Info Actions
    onUpdateDisplayName: displayName => dispatch(Actions.updateDisplayName(displayName)),
    onUpdateDatabricksToken: databricksToken => dispatch(Actions.updateDatabricksToken(databricksToken)),

    // Input Actions
    onUpdateInputMode: mode => dispatch(Actions.updateInputMode(mode)),
    onUpdateInputType: type => dispatch(Actions.updateInputType(type)),
    onUpdateInputHubName: name => dispatch(Actions.updateInputHubName(name)),
    onUpdateInputPath: path => dispatch(Actions.updateInputPath(path)),
    onUpdateInputHubConnection: connection => dispatch(Actions.updateInputHubConnection(connection)),
    onUpdateInputSubscriptionId: id => dispatch(Actions.updateInputSubscriptionId(id)),
    onUpdateInputResourceGroup: name => dispatch(Actions.updateInputResourceGroup(name)),

    onUpdateBatchInputConnection: connection => dispatch(Actions.updateBatchInputConnection(connection)),
    onUpdateBatchInputPath: path => dispatch(Actions.updateBlobInputPath(path)),
    onUpdateBatchInputFormatType: formatType => dispatch(Actions.updateBatchInputFormatType(formatType)),
    onUpdateBatchInputCompressionType: compressionType => dispatch(Actions.updateBachInputCompressionType(compressionType)),

    onUpdateInputWindowDuration: duration => dispatch(Actions.updateInputWindowDuration(duration)),
    onUpdateInputTimestampColumn: duration => dispatch(Actions.updateInputTimestampColumn(duration)),
    onUpdateInputWatermarkValue: duration => dispatch(Actions.updateInputWatermarkValue(duration)),
    onUpdateInputWatermarkUnit: duration => dispatch(Actions.updateInputWatermarkUnit(duration)),
    onUpdateInputMaxRate: maxRate => dispatch(Actions.updateInputMaxRate(maxRate)),
    onUpdateInputSchema: schema => dispatch(Actions.updateInputSchema(schema)),
    onGetInputSchema: flow => dispatch(Actions.getInputSchema(flow)),
    onUpdateShowNormalizationSnippet: show => dispatch(Actions.updateShowNormalizationSnippet(show)),
    onUpdateNormalizationSnippet: snippet => dispatch(Actions.updateNormalizationSnippet(snippet)),
    onUpdateSamplingInputDuration: duration => dispatch(Actions.updateSamplingInputDuration(duration)),

    // Reference Data Actions
    onNewReferenceData: type => dispatch(Actions.newReferenceData(type)),
    onDeleteReferenceData: index => dispatch(Actions.deleteReferenceData(index)),
    onUpdateSelectedReferenceDataIndex: index => dispatch(Actions.updateSelectedReferenceDataIndex(index)),
    onUpdateReferenceDataName: name => dispatch(Actions.updateReferenceDataName(name)),
    onUpdateCsvPath: path => dispatch(Actions.updateCsvPath(path)),
    onUpdateCsvDelimiter: delimiter => dispatch(Actions.updateCsvDelimiter(delimiter)),
    onUpdateCsvContainsHeader: header => dispatch(Actions.updateCsvContainsHeader(header)),

    // Functions Actions
    onNewFunction: type => dispatch(Actions.newFunction(type)),
    onDeleteFunction: index => dispatch(Actions.deleteFunction(index)),
    onUpdateSelectedFunctionIndex: index => dispatch(Actions.updateSelectedFunctionIndex(index)),
    onUpdateFunctionName: name => dispatch(Actions.updateFunctionName(name)),
    onUpdateUdfPath: path => dispatch(Actions.updateUdfPath(path)),
    onUpdateUdfClass: name => dispatch(Actions.updateUdfClass(name)),
    onUpdateUdfDependencyLibs: libs => dispatch(Actions.updateUdfDependencyLibs(libs)),

    onUpdateAzureFunctionServiceEndpoint: serviceEndpoint => dispatch(Actions.updateAzureFunctionServiceEndpoint(serviceEndpoint)),
    onUpdateAzureFunctionApi: api => dispatch(Actions.updateAzureFunctionApi(api)),
    onUpdateAzureFunctionCode: code => dispatch(Actions.updateAzureFunctionCode(code)),
    onUpdateAzureFunctionMethodType: methodType => dispatch(Actions.updateAzureFunctionMethodType(methodType)),
    onUpdateAzureFunctionParams: params => dispatch(Actions.updateAzureFunctionParams(params)),

    // Query Actions
    onUpdateQuery: query => dispatch(QueryActions.updateQuery(query)),
    onGetCodeGenQuery: queryMetadata => QueryActions.getCodeGenQuery(queryMetadata),
    onExecuteQuery: (queryMetadata, selectedQuery, kernelId) => dispatch(QueryActions.executeQuery(queryMetadata, selectedQuery, kernelId)),
    onGetKernel: (queryMetadata, version, updateErrorMessage) =>
        dispatch(KernelActions.getKernel(queryMetadata, version, updateErrorMessage)),
    onUpdateKernelVersion: version => dispatch(KernelActions.updateKernelVersion(version)),
    onRefreshKernel: (queryMetadata, kernelId, version, updateErrorMessage) =>
        dispatch(KernelActions.refreshKernel(queryMetadata, kernelId, version, updateErrorMessage)),
    onDeleteKernel: (kernelId, version, flowName) => dispatch(KernelActions.deleteKernel(kernelId, version, flowName)),
    onResampleInput: (queryMetadata, kernelId, version) => dispatch(QueryActions.resampleInput(queryMetadata, kernelId, version)),
    onUpdateResamplingInputDuration: duration => dispatch(QueryActions.updateResamplingInputDuration(duration)),
    onDeleteAllKernels: (updateErrorMessage, flowName) => dispatch(KernelActions.deleteAllKernels(updateErrorMessage, flowName)),

    // Query Pane Layout Actions
    onShowTestQueryOutputPanel: isVisible => dispatch(LayoutActions.onShowTestQueryOutputPanel(isVisible)),

    // Scale Actions
    onUpdateNumExecutors: numExecutors => dispatch(Actions.updateNumExecutors(numExecutors)),
    onUpdateExecutorMemory: executorMemory => dispatch(Actions.updateExecutorMemory(executorMemory)),
    onUpdateDatabricksAutoScale: databricksAutoScale => dispatch(Actions.updateDatabricksAutoScale(databricksAutoScale)),
    onUpdateDatabricksMinWorkers: databricksMinWorkers => dispatch(Actions.updateDatabricksMinWorkers(databricksMinWorkers)),
    onUpdateDatabricksMaxWorkers: databricksMaxWorkers => dispatch(Actions.updateDatabricksMaxWorkers(databricksMaxWorkers)),

    // Output Actions
    onNewSinker: type => dispatch(Actions.newSinker(type)),
    onDeleteSinker: index => dispatch(Actions.deleteSinker(index)),
    onUpdateSelectedSinkerIndex: index => dispatch(Actions.updateSelectedSinkerIndex(index)),
    onUpdateSinkerName: name => dispatch(Actions.updateSinkerName(name)),
    onUpdateCosmosDbConnection: connection => dispatch(Actions.updateCosmosDbConnection(connection)),
    onUpdateCosmosDbDatabase: database => dispatch(Actions.updateCosmosDbDatabase(database)),
    onUpdateCosmosDbCollection: collection => dispatch(Actions.updateCosmosDbCollection(collection)),
    onUpdateEventHubConnection: connection => dispatch(Actions.updateEventHubConnection(connection)),
    onUpdateBlobConnection: connection => dispatch(Actions.updateBlobConnection(connection)),
    onUpdateBlobContainerName: name => dispatch(Actions.updateBlobContainerName(name)),
    onUpdateBlobPrefix: prefix => dispatch(Actions.updateBlobPrefix(prefix)),
    onUpdateBlobPartitionFormat: format => dispatch(Actions.updateBlobPartitionFormat(format)),
    onUpdateFormatType: type => dispatch(Actions.updateFormatType(type)),
    onUpdateCompressionType: type => dispatch(Actions.updateCompressionType(type)),

    onUpdateSqlConnection: connection => dispatch(Actions.updateSqlConnection(connection)),
    onUpdateSqlTableName: name => dispatch(Actions.updateSqlTableName(name)),
    onUpdateSqlWriteMode: mode => dispatch(Actions.updateSqlWriteMode(mode)),
    onUpdateSqlUseBulkInsert: useBulkInsert => dispatch(Actions.updateSqlUseBulkInsert(useBulkInsert)),

    // Rule Actions
    onNewRule: type => dispatch(Actions.newRule(type)),
    onDeleteRule: index => dispatch(Actions.deleteRule(index)),
    onUpdateSelectedRuleIndex: index => dispatch(Actions.updateSelectedRuleIndex(index)),
    onUpdateRuleName: name => dispatch(Actions.updateRuleName(name)),
    onUpdateTagRuleSubType: type => dispatch(Actions.updateTagRuleSubType(type)),
    onUpdateTagRuleDescription: description => dispatch(Actions.updateTagRuleDescription(description)),
    onUpdateTagTag: tag => dispatch(Actions.updateTagTag(tag)),
    onUpdateTagIsAlert: isAlert => dispatch(Actions.updateTagIsAlert(isAlert)),
    onUpdateTagSinks: sinks => dispatch(Actions.updateTagSinks(sinks)),
    onUpdateTagSeverity: severity => dispatch(Actions.updateTagSeverity(severity)),
    onUpdateTagConditions: conditions => dispatch(Actions.updateTagConditions(conditions)),
    onUpdateTagAggregates: aggregates => dispatch(Actions.updateTagAggregates(aggregates)),
    onUpdateTagPivots: pivots => dispatch(Actions.updateTagPivots(pivots)),
    onUpdateSchemaTableName: name => dispatch(Actions.updateSchemaTableName(name)),
    onGetTableSchemas: queryMetadata => QueryActions.getTableSchemas(queryMetadata),

    // Save and Delete Actions
    onSaveFlow: (flow, query) => Actions.saveFlow(flow, query),
    onDeployFlow: (flow, query) => Actions.deployFlow(flow, query),
    onDeleteFlow: flow => Actions.deleteFlow(flow),

    // enableOneBox Action
    onUpdateOneBoxMode: enableOneBox => dispatch(Actions.updateOneBoxMode(enableOneBox)),

    // Schedule Actions
    onNewBatch: type => dispatch(Actions.newBatch(type)),
    onDeleteBatch: index => dispatch(Actions.deleteBatch(index)),
    onUpdateSelectedBatchIndex: index => dispatch(Actions.updateSelectedBatchIndex(index)),
    onUpdateBatchName: value => dispatch(Actions.updateBatchName(value)),
    onUpdateBatchStartTime: value => dispatch(Actions.updateBatchStartTime(value)),
    onUpdateBatchEndTime: value => dispatch(Actions.updateBatchEndTime(value)),
    onUpdateBatchIntervalValue: value => dispatch(Actions.updateBatchIntervalValue(value)),
    onUpdateBatchIntervalType: value => dispatch(Actions.updateBatchIntervalType(value)),
    onUpdateBatchDelayValue: value => dispatch(Actions.updateBatchDelayValue(value)),
    onUpdateBatchDelayType: value => dispatch(Actions.updateBatchDelayType(value)),
    onUpdateBatchWindowValue: value => dispatch(Actions.updateBatchWindowValue(value)),
    onUpdateBatchWindowType: value => dispatch(Actions.updateBatchWindowType(value))
});

// Styles
const contentStyle = {
    paddingTop: 20,
    paddingLeft: 20,
    paddingRight: 20,
    paddingBottom: 20,
    backgroundColor: Colors.neutralTertiaryAlt,
    display: 'flex',
    flexDirection: 'column',
    overflowX: 'hidden',
    overflowY: 'hidden',
    flex: 1
};

const tabContainerStyle = {
    display: 'flex',
    flexDirection: 'column',
    overflowX: 'hidden',
    overflowY: 'hidden',
    flex: 1
};

const modelHeaderStyle = {
    background: Colors.themePrimary,
    color: Colors.white,
    padding: '20px 30px'
};

const modalBodyStyle = {
    flex: 1,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    height: '150px'
};

export default withRouter(
    connect(
        mapStateToProps,
        mapDispatchToProps
    )(FlowDefinitionPanel)
);
