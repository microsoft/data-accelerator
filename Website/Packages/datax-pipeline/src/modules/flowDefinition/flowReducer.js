// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import * as Actions from './flowActions';
import * as Models from './flowModels';
import { QueryModels, QueryActions } from 'datax-query';

const INITIAL_FLOW_STATE = {
    // Flow Config
    name: '',
    flowId: '',
    displayName: Models.getDefaultName(),
    owner: '',
    databricksToken: '',
    input: Models.defaultInput,
    referenceData: [],
    functions: [],
    query: QueryModels.defaultQuery,
    scale: {
        jobNumExecutors: '4',
        jobExecutorMemory: '1000',
        jobDatabricksAutoScale: false,
        jobDatabricksMinWorkers: '3',
        jobDatabricksMaxWorkers: '8'
    },
    outputs: [Models.getMetricSinker()],
    outputTemplates: [],
    rules: [],

    batchInputs: [Models.getDefaultBatchInputSettings()],
    batchList: [],

    // State
    isNew: false,
    isDirty: false,
    selectedFlowBatchInputIndex: undefined,
    selectedBatchIndex: undefined,
    selectedReferenceDataIndex: undefined,
    selectedFunctionIndex: undefined,
    selectedSinkerIndex: undefined,
    selectedOutputTemplateIndex: undefined,
    selectedRuleIndex: undefined,
    fetchingInputSchema: false,
    samplingInputDuration: '15',
    resamplingInputDuration: '15',
    errorMessage: undefined,
    warningMessage: undefined,
    enableLocalOneBox: false,
    isDatabricksSparkType: false
};

export default (state = INITIAL_FLOW_STATE, action) => {
    switch (action.type) {
        case Actions.FLOW_INIT:
            const flow = action.payload;
            return Object.assign({}, INITIAL_FLOW_STATE, flow, {
                isNew: false,
                isDirty: false,
                selectedFlowBatchInputIndex: flow.batchInputs && flow.batchInputs.length > 0 ? 0 : undefined,
                selectedReferenceDataIndex: flow.referenceData && flow.referenceData.length > 0 ? 0 : undefined,
                selectedFunctionIndex: flow.functions && flow.functions.length > 0 ? 0 : undefined,
                selectedSinkerIndex: flow.outputs && flow.outputs.length > 0 ? 0 : undefined,
                selectedOutputTemplateIndex: flow.outputTemplates && flow.outputTemplates.length > 0 ? 0 : undefined,
                selectedRuleIndex: flow.rules && flow.rules.length > 0 ? 0 : undefined,
                enableLocalOneBox: state.enableLocalOneBox
            });

        case Actions.FLOW_NEW:
            return Object.assign({}, INITIAL_FLOW_STATE, {
                isNew: true,
                isDirty: true,
                owner: action.payload,
                selectedSinkerIndex: 0, // new flow by default contains the metric sinker
                selectedFlowBatchInputIndex: 0,
                enableLocalOneBox: state.enableLocalOneBox ? state.enableLocalOneBox : false,
                input: Models.getDefaultInput(state.enableLocalOneBox),
                query: QueryModels.getDefaultQuery(state.enableLocalOneBox)
            });

        case Actions.FLOW_UPDATE_DISPLAY_NAME:
            return Object.assign({}, state, {
                isDirty: true,
                displayName: action.payload
            });

        case Actions.FLOW_UPDATE_DATABRICKSTOKEN:
            return Object.assign({}, state, {
                isDirty: true,
                databricksToken: action.payload
            });

        case Actions.FLOW_UPDATE_ISDATABRICKSSPARKTYPE:
            return Object.assign({}, state, {
                isDatabricksSparkType: action.payload
            });

        case Actions.FLOW_UPDATE_OWNER:
            return Object.assign({}, state, { owner: action.payload });

        case Actions.FLOW_UPDATE_INPUT:
            return Object.assign({}, state, {
                isDirty: true,
                input: action.payload
            });

        case Actions.FLOW_UPDATE_BATCH_INPUT:
            let updatedBatchInputs = [...state.batchInputs];
            updatedBatchInputs[action.index] = action.payload;
            return Object.assign({}, state, {
                isDirty: true,
                batchInputs: updatedBatchInputs
            });

        case Actions.FLOW_UPDATE_SAMPLING_INPUT_DURATION:
            return Object.assign({}, state, {
                samplingInputDuration: action.duration
            });

        case Actions.FLOW_UPDATE_REFERENCE_DATA_LIST:
            return Object.assign({}, state, {
                isDirty: true,
                referenceData: action.payload
            });

        case Actions.FLOW_NEW_REFERENCE_DATA:
            const referenceDataList = [...state.referenceData, Models.getDefaultReferenceDataSettings(action.payload)];
            return Object.assign({}, state, {
                isDirty: true,
                referenceData: referenceDataList,
                selectedReferenceDataIndex: referenceDataList.length - 1
            });

        case Actions.FLOW_UPDATE_REFERENCE_DATA:
            let updatedReferenceDataList = [...state.referenceData];
            updatedReferenceDataList[action.index] = action.payload;
            return Object.assign({}, state, {
                isDirty: true,
                referenceData: updatedReferenceDataList
            });

        case Actions.FLOW_DELETE_REFERENCE_DATA:
            const deleteReferenceDataIndex = action.index;
            let referenceDataListAfterDelete = [...state.referenceData];
            referenceDataListAfterDelete.splice(deleteReferenceDataIndex, 1);

            let nextReferenceDataIndex;
            if (referenceDataListAfterDelete.length > deleteReferenceDataIndex) {
                nextReferenceDataIndex = deleteReferenceDataIndex;
            } else if (referenceDataListAfterDelete.length > 0) {
                nextReferenceDataIndex = deleteReferenceDataIndex - 1;
            } else {
                nextReferenceDataIndex = undefined;
            }

            return Object.assign({}, state, {
                isDirty: true,
                referenceData: referenceDataListAfterDelete,
                selectedReferenceDataIndex: nextReferenceDataIndex
            });

        case Actions.FLOW_UPDATE_SELECTED_REFERENCE_DATA_INDEX:
            return Object.assign({}, state, { selectedReferenceDataIndex: action.payload });

        case Actions.FLOW_UPDATE_FUNCTIONS:
            return Object.assign({}, state, {
                isDirty: true,
                functions: action.payload
            });

        case Actions.FLOW_NEW_FUNCTION:
            const functions = [...state.functions, Models.getDefaultFunctionSettings(action.payload)];
            return Object.assign({}, state, {
                isDirty: true,
                functions: functions,
                selectedFunctionIndex: functions.length - 1
            });

        case Actions.FLOW_UPDATE_FUNCTION:
            let updatedFunctions = [...state.functions];
            updatedFunctions[action.index] = action.payload;
            return Object.assign({}, state, {
                isDirty: true,
                functions: updatedFunctions
            });

        case Actions.FLOW_DELETE_FUNCTION:
            const deleteFunctionIndex = action.index;
            let functionsAfterDelete = [...state.functions];
            functionsAfterDelete.splice(deleteFunctionIndex, 1);

            let nextFunctionIndex;
            if (functionsAfterDelete.length > deleteFunctionIndex) {
                nextFunctionIndex = deleteFunctionIndex;
            } else if (functionsAfterDelete.length > 0) {
                nextFunctionIndex = deleteFunctionIndex - 1;
            } else {
                nextFunctionIndex = undefined;
            }

            return Object.assign({}, state, {
                isDirty: true,
                functions: functionsAfterDelete,
                selectedFunctionIndex: nextFunctionIndex
            });

        case Actions.FLOW_UPDATE_SELECTED_FUNCTION_INDEX:
            return Object.assign({}, state, { selectedFunctionIndex: action.payload });

        case Actions.FLOW_UPDATE_SCALE:
            return Object.assign({}, state, {
                isDirty: true,
                scale: action.payload
            });

        case Actions.FLOW_UPDATE_OUTPUTS:
            return Object.assign({}, state, {
                isDirty: true,
                outputs: action.payload
            });

        case Actions.FLOW_NEW_SINKER:
            const outputs = [...state.outputs, Models.getDefaultSinkerSettings(action.payload, state.owner)];
            return Object.assign({}, state, {
                isDirty: true,
                outputs: outputs,
                selectedSinkerIndex: outputs.length - 1
            });

        case Actions.FLOW_UPDATE_SINKER:
            let updatedSinkers = [...state.outputs];
            updatedSinkers[action.index] = action.payload;
            return Object.assign({}, state, {
                isDirty: true,
                outputs: updatedSinkers
            });

        case Actions.FLOW_DELETE_SINKER:
            const deleteSinkerIndex = action.index;
            let sinkersAfterDelete = [...state.outputs];
            sinkersAfterDelete.splice(deleteSinkerIndex, 1);

            let nextSinkerIndex;
            if (sinkersAfterDelete.length > deleteSinkerIndex) {
                nextSinkerIndex = deleteSinkerIndex;
            } else if (sinkersAfterDelete.length > 0) {
                nextSinkerIndex = deleteSinkerIndex - 1;
            } else {
                nextSinkerIndex = undefined;
            }

            return Object.assign({}, state, {
                isDirty: true,
                outputs: sinkersAfterDelete,
                selectedSinkerIndex: nextSinkerIndex
            });

        case Actions.FLOW_UPDATE_SELECTED_SINKER_INDEX:
            return Object.assign({}, state, { selectedSinkerIndex: action.payload });

        case Actions.FLOW_UPDATE_RULES:
            return Object.assign({}, state, {
                isDirty: true,
                rules: action.payload
            });

        case Actions.FLOW_NEW_RULE:
            const rules = [...state.rules, Models.getDefaultRuleSettings(action.payload)];
            return Object.assign({}, state, {
                isDirty: true,
                rules: rules,
                selectedRuleIndex: rules.length - 1
            });

        case Actions.FLOW_UPDATE_RULE:
            let updatedRules = [...state.rules];
            updatedRules[action.index] = action.payload;
            return Object.assign({}, state, {
                isDirty: true,
                rules: updatedRules
            });

        case Actions.FLOW_DELETE_RULE:
            const deleteRuleIndex = action.index;
            let rulesAfterDelete = [...state.rules];
            rulesAfterDelete.splice(deleteRuleIndex, 1);

            let nextRuleIndex;
            if (rulesAfterDelete.length > deleteRuleIndex) {
                nextRuleIndex = deleteRuleIndex;
            } else if (rulesAfterDelete.length > 0) {
                nextRuleIndex = deleteRuleIndex - 1;
            } else {
                nextRuleIndex = undefined;
            }

            return Object.assign({}, state, {
                isDirty: true,
                rules: rulesAfterDelete,
                selectedRuleIndex: nextRuleIndex
            });

        case Actions.FLOW_UPDATE_SELECTED_RULE_INDEX:
            return Object.assign({}, state, { selectedRuleIndex: action.payload });

        case Actions.FLOW_FETCHING_INPUT_SCHEMA:
            return Object.assign({}, state, {
                fetchingInputSchema: action.value
            });

        case Actions.FLOW_UPDATE_ERROR_MESSAGE:
            return Object.assign({}, state, { errorMessage: action.message });

        case Actions.FLOW_UPDATE_WARNING_MESSAGE:
            return Object.assign({}, state, { warningMessage: action.message });

        case Actions.FLOW_UPDATE_ONEBOX_MODE:
            return Object.assign({}, state, {
                enableLocalOneBox: action.payload
            });

        // Batch
        case Actions.FLOW_NEW_BATCH:
            const batchList = [...state.batchList, Models.getDefaultBatchSettings(action.payload)];
            return Object.assign({}, state, {
                isDirty: true,
                batchList: batchList,
                selectedBatchIndex: batchList.length - 1
            });

        case Actions.FLOW_DELETE_BATCH:
            const deleteBatchIndex = action.index;
            let batchListAfterDelete = [...state.batchList];
            batchListAfterDelete.splice(deleteBatchIndex, 1);

            let nextBatchIndex;
            if (batchListAfterDelete.length > deleteBatchIndex) {
                nextBatchIndex = deleteBatchIndex;
            } else if (batchListAfterDelete.length > 0) {
                nextBatchIndex = deleteBatchIndex - 1;
            } else {
                nextBatchIndex = undefined;
            }

            return Object.assign({}, state, {
                isDirty: true,
                batchList: batchListAfterDelete,
                selectedBatchIndex: nextBatchIndex
            });

        case Actions.FLOW_UPDATE_SELECTED_BATCH_INDEX:
            return Object.assign({}, state, { selectedBatchIndex: action.payload });

        case Actions.FLOW_UPDATE_BATCHLIST:
            let updatedBatchList = [...state.batchList];
            updatedBatchList[action.index] = action.payload;
            return Object.assign({}, state, {
                isDirty: true,
                batchList: updatedBatchList
            });

        default:
            return state;
    }
};
