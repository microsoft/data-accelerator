// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import Q from 'q';
import { UserSelectors, getApiErrorMessage } from 'datax-common';

import * as Api from './api';
import * as Helpers from './flowHelpers';
import * as Selectors from './flowSelectors';
import * as KernelActions from './kernelActions';
import * as KernelSelectors from './kernelSelectors';

/**
 *
 * REDUX Action Types
 *
 */

// Init
export const FLOW_INIT = 'FLOW_INIT';
export const FLOW_NEW = 'FLOW_NEW';

// Info
export const FLOW_UPDATE_DISPLAY_NAME = 'FLOW_UPDATE_DISPLAY_NAME';
export const FLOW_UPDATE_OWNER = 'FLOW_UPDATE_OWNER';
export const FLOW_UPDATE_DATABRICKSTOKEN = 'FLOW_UPDATE_DATABRICKSTOKEN';

// Query
export const FLOW_UPDATE_QUERY = 'FLOW_UPDATE_QUERY';

// Scale
export const FLOW_UPDATE_SCALE = 'FLOW_UPDATE_SCALE';

// Outputs
export const FLOW_UPDATE_OUTPUTS = 'FLOW_UPDATE_OUTPUTS';
export const FLOW_NEW_SINKER = 'FLOW_NEW_SINKER';
export const FLOW_DELETE_SINKER = 'FLOW_DELETE_SINKER';
export const FLOW_UPDATE_SINKER = 'FLOW_UPDATE_SINKER';
export const FLOW_UPDATE_SELECTED_SINKER_INDEX = 'FLOW_UPDATE_SELECTED_SINKER_INDEX';

// Rules
export const FLOW_UPDATE_RULES = 'FLOW_UPDATE_RULES';
export const FLOW_NEW_RULE = 'FLOW_NEW_RULE';
export const FLOW_DELETE_RULE = 'FLOW_DELETE_RULE';
export const FLOW_UPDATE_RULE = 'FLOW_UPDATE_RULE';
export const FLOW_UPDATE_SELECTED_RULE_INDEX = 'FLOW_UPDATE_SELECTED_RULE_INDEX';

// Reference Data
export const FLOW_UPDATE_REFERENCE_DATA_LIST = 'FLOW_UPDATE_REFERENCE_DATA_LIST';
export const FLOW_NEW_REFERENCE_DATA = 'FLOW_NEW_REFERENCE_DATA';
export const FLOW_UPDATE_REFERENCE_DATA = 'FLOW_UPDATE_REFERENCE_DATA';
export const FLOW_DELETE_REFERENCE_DATA = 'FLOW_DELETE_REFERENCE_DATA';
export const FLOW_UPDATE_SELECTED_REFERENCE_DATA_INDEX = 'FLOW_UPDATE_SELECTED_REFERENCE_DATA_INDEX';

// Fuctions
export const FLOW_UPDATE_FUNCTIONS = 'FLOW_UPDATE_FUNCTIONS';
export const FLOW_NEW_FUNCTION = 'FLOW_NEW_FUNCTION';
export const FLOW_UPDATE_FUNCTION = 'FLOW_UPDATE_FUNCTION';
export const FLOW_DELETE_FUNCTION = 'FLOW_DELETE_FUNCTION';
export const FLOW_UPDATE_SELECTED_FUNCTION_INDEX = 'FLOW_UPDATE_SELECTED_FUNCTION_INDEX';

// Input
export const FLOW_UPDATE_INPUT = 'FLOW_UPDATE_INPUT';
export const FLOW_FETCHING_INPUT_SCHEMA = 'FLOW_FETCHING_INPUT_SCHEMA';
export const FLOW_UPDATE_SAMPLING_INPUT_DURATION = 'FLOW_UPDATE_SAMPLING_INPUT_DURATION';
export const FLOW_UPDATE_RESAMPLING_INPUT_DURATION = 'FLOW_UPDATE_RESAMPLING_INPUT_DURATION';

// Message
export const FLOW_UPDATE_ERROR_MESSAGE = 'FLOW_UPDATE_ERROR_MESSAGE';
export const FLOW_UPDATE_WARNING_MESSAGE = 'FLOW_UPDATE_WARNING_MESSAGE';

// OneBox
export const FLOW_UPDATE_ONEBOX_MODE = 'FLOW_UPDATE_ONEBOX_MODE';

/**
 *
 * REDUX Action Implementations
 *
 */

// Init Actions
export const initFlow = context => (dispatch, getState) => {
    if (context && context.id) {
        return Api.getFlow(context.id)
            .then(config => {
                const flow = Helpers.convertConfigToFlow(config);
                return dispatch({
                    type: FLOW_INIT,
                    payload: flow
                });
            })
            .catch(error => {
                const message = getApiErrorMessage(error);
                updateErrorMessage(dispatch, message);
                return Q.reject({ error: true, message: message });
            });
    } else {
        const owner = UserSelectors.getUserAlias(getState());
        return dispatch({
            type: FLOW_NEW,
            payload: owner
        });
    }
};

// Info Actions
export const updateDisplayName = displayName => dispatch => {
    return dispatch({
        type: FLOW_UPDATE_DISPLAY_NAME,
        payload: displayName
    });
};

export const updateOwner = () => (dispatch, getState) => {
    return dispatch({
        type: FLOW_UPDATE_OWNER,
        payload: UserSelectors.getUserAlias(getState())
    });
};

export const updateDatabricksToken = databricksToken => dispatch => {
    return dispatch({
        type: FLOW_UPDATE_DATABRICKSTOKEN,
        payload: databricksToken
    });
};

// Input Actions
export const updateInputMode = mode => (dispatch, getState) => {
    updateInput(
        dispatch,
        Object.assign({}, Selectors.getFlowInput(getState()), {
            mode: mode
        })
    );
};

export const updateInputType = type => (dispatch, getState) => {
    updateInput(
        dispatch,
        Object.assign({}, Selectors.getFlowInput(getState()), {
            type: type
        })
    );
};

export const updateInputHubName = name => (dispatch, getState) => {
    updateInputProperties(dispatch, getState, {
        inputEventhubName: name
    });
};

export const updateInputHubConnection = connection => (dispatch, getState) => {
    updateInputProperties(dispatch, getState, {
        inputEventhubConnection: connection
    });
};

export const updateInputSubscriptionId = id => (dispatch, getState) => {
    updateInputProperties(dispatch, getState, {
        inputSubscriptionId: id
    });
};

export const updateInputResourceGroup = name => (dispatch, getState) => {
    updateInputProperties(dispatch, getState, {
        inputResourceGroup: name
    });
};

export const updateInputWindowDuration = duration => (dispatch, getState) => {
    updateInputProperties(dispatch, getState, {
        windowDuration: duration
    });
};

export const updateInputTimestampColumn = column => (dispatch, getState) => {
    updateInputProperties(dispatch, getState, {
        timestampColumn: column
    });
};

export const updateInputWatermarkValue = watermarkValue => (dispatch, getState) => {
    updateInputProperties(dispatch, getState, {
        watermarkValue: watermarkValue
    });
};

export const updateInputWatermarkUnit = watermarkUnit => (dispatch, getState) => {
    updateInputProperties(dispatch, getState, {
        watermarkUnit: watermarkUnit
    });
};

export const updateInputMaxRate = maxRate => (dispatch, getState) => {
    updateInputProperties(dispatch, getState, {
        maxRate: maxRate
    });
};

export const updateShowNormalizationSnippet = show => (dispatch, getState) => {
    updateInputProperties(dispatch, getState, {
        showNormalizationSnippet: show
    });
};

export const updateNormalizationSnippet = snippet => (dispatch, getState) => {
    updateInputProperties(dispatch, getState, {
        normalizationSnippet: snippet
    });
};

export const updateSamplingInputDuration = duration => dispatch => {
    return dispatch({
        type: FLOW_UPDATE_SAMPLING_INPUT_DURATION,
        duration: duration
    });
};

export const updateResamplingInputDuration = duration => dispatch => {
    return dispatch({
        type: FLOW_UPDATE_RESAMPLING_INPUT_DURATION,
        duration: duration
    });
};

export const updateInputSchema = schema => (dispatch, getState) => {
    updateInputProperties(dispatch, getState, {
        inputSchemaFile: schema
    });
};

export const getInputSchema = flow => (dispatch, getState) => {
    fetchingInputSchema(dispatch, true);
    dispatch(updateWarningMessage(undefined));
    return Api.getInputSchema(flow)
        .then(response => {
            const schema = response.Schema;
            const message = response.Errors === undefined ? undefined : response.Errors.join('. ');
            updateInputProperties(dispatch, getState, {
                inputSchemaFile: schema
            });

            dispatch(updateWarningMessage(message));
            fetchingInputSchema(dispatch, false);
        })
        .catch(error => {
            updateInputProperties(dispatch, getState, {
                inputSchemaFile: '{}'
            });

            fetchingInputSchema(dispatch, false);

            const message = getApiErrorMessage(error);
            return Q.reject({ error: true, message: message });
        });
};

function fetchingInputSchema(dispatch, value) {
    return dispatch({
        type: FLOW_FETCHING_INPUT_SCHEMA,
        value: value
    });
}

function updateInputProperties(dispatch, getState, propertyMember) {
    updateInput(
        dispatch,
        Object.assign({}, Selectors.getFlowInput(getState()), {
            properties: Object.assign({}, Selectors.getFlowInputProperties(getState()), propertyMember)
        })
    );
}

function updateInput(dispatch, input) {
    return dispatch({
        type: FLOW_UPDATE_INPUT,
        payload: input
    });
}

// Reference Data Actions
export const updateReferenceDataList = items => dispatch => {
    return dispatch({
        type: FLOW_UPDATE_REFERENCE_DATA_LIST,
        payload: items
    });
};

export const newReferenceData = type => dispatch => {
    return dispatch({
        type: FLOW_NEW_REFERENCE_DATA,
        payload: type
    });
};

export const deleteReferenceData = index => dispatch => {
    return dispatch({
        type: FLOW_DELETE_REFERENCE_DATA,
        index: index
    });
};

export const updateSelectedReferenceDataIndex = index => dispatch => {
    return dispatch({
        type: FLOW_UPDATE_SELECTED_REFERENCE_DATA_INDEX,
        payload: index
    });
};

export const updateReferenceDataName = name => (dispatch, getState) => {
    updateReferenceData(
        dispatch,
        Selectors.getSelectedReferenceDataIndex(getState()),
        Object.assign({}, Selectors.getSelectedReferenceData(getState()), {
            id: name
        })
    );
};

// CSV Reference Data
export const updateCsvPath = path => (dispatch, getState) => {
    updateReferenceDataProperties(dispatch, getState, {
        path: path
    });
};

export const updateCsvDelimiter = delimiter => (dispatch, getState) => {
    updateReferenceDataProperties(dispatch, getState, {
        delimiter: delimiter
    });
};

export const updateCsvContainsHeader = header => (dispatch, getState) => {
    updateReferenceDataProperties(dispatch, getState, {
        header: header
    });
};

function updateReferenceDataProperties(dispatch, getState, propertyMember) {
    updateReferenceData(
        dispatch,
        Selectors.getSelectedReferenceDataIndex(getState()),
        Object.assign({}, Selectors.getSelectedReferenceData(getState()), {
            properties: Object.assign({}, Selectors.getSelectedReferenceDataProperties(getState()), propertyMember)
        })
    );
}

function updateReferenceData(dispatch, index, referenceData) {
    return dispatch({
        type: FLOW_UPDATE_REFERENCE_DATA,
        payload: referenceData,
        index: index
    });
}

// Function Actions
export const updateFunctions = items => dispatch => {
    return dispatch({
        type: FLOW_UPDATE_FUNCTIONS,
        payload: items
    });
};

export const newFunction = type => dispatch => {
    return dispatch({
        type: FLOW_NEW_FUNCTION,
        payload: type
    });
};

export const deleteFunction = index => dispatch => {
    return dispatch({
        type: FLOW_DELETE_FUNCTION,
        index: index
    });
};

export const updateSelectedFunctionIndex = index => dispatch => {
    return dispatch({
        type: FLOW_UPDATE_SELECTED_FUNCTION_INDEX,
        payload: index
    });
};

export const updateFunctionName = name => (dispatch, getState) => {
    updateFunction(
        dispatch,
        Selectors.getSelectedFunctionIndex(getState()),
        Object.assign({}, Selectors.getSelectedFunction(getState()), {
            id: name
        })
    );
};

// UDF/UDAF
export const updateUdfPath = path => (dispatch, getState) => {
    updateFunctionProperties(dispatch, getState, {
        path: path
    });
};

export const updateUdfClass = name => (dispatch, getState) => {
    updateFunctionProperties(dispatch, getState, {
        class: name
    });
};

export const updateUdfDependencyLibs = libs => (dispatch, getState) => {
    updateFunctionProperties(dispatch, getState, {
        libs: libs
    });
};

// Azure Functions
export const updateAzureFunctionServiceEndpoint = serviceEndpoint => (dispatch, getState) => {
    updateFunctionProperties(dispatch, getState, {
        serviceEndpoint: serviceEndpoint
    });
};

export const updateAzureFunctionApi = api => (dispatch, getState) => {
    updateFunctionProperties(dispatch, getState, {
        api: api
    });
};

export const updateAzureFunctionCode = code => (dispatch, getState) => {
    updateFunctionProperties(dispatch, getState, {
        code: code
    });
};

export const updateAzureFunctionMethodType = methodType => (dispatch, getState) => {
    updateFunctionProperties(dispatch, getState, {
        methodType: methodType
    });
};

export const updateAzureFunctionParams = params => (dispatch, getState) => {
    updateFunctionProperties(dispatch, getState, {
        params: params
    });
};

function updateFunctionProperties(dispatch, getState, propertyMember) {
    updateFunction(
        dispatch,
        Selectors.getSelectedFunctionIndex(getState()),
        Object.assign({}, Selectors.getSelectedFunction(getState()), {
            properties: Object.assign({}, Selectors.getSelectedFunctionProperties(getState()), propertyMember)
        })
    );
}

function updateFunction(dispatch, index, functionItem) {
    return dispatch({
        type: FLOW_UPDATE_FUNCTION,
        payload: functionItem,
        index: index
    });
}

// Query Actions
export const updateQuery = query => dispatch => {
    return dispatch({
        type: FLOW_UPDATE_QUERY,
        payload: query
    });
};

export const getTableSchemas = flow => {
    return Api.getTableSchemas(flow).then(tables => {
        let tableToSchemaMap = {};
        tables.forEach(table => {
            tableToSchemaMap[table.name] = table;
        });

        return tableToSchemaMap;
    });
};

export const getCodeGenQuery = flow => {
    return Api.getCodeGenQuery(flow).then(query => {
        return query;
    });
};

export const executeQuery = (flow, selectedQuery, kernelId) => dispatch => {
    updateErrorMessage(dispatch, undefined);
    return Api.executeQuery(flow, selectedQuery, kernelId)
        .then(result => {
            return result;
        })
        .catch(error => {
            const message = getApiErrorMessage(error);
            updateErrorMessage(dispatch, message);
            return Q.reject({ error: true, message: message });
        });
};

export const resampleInput = (flow, kernelId, version) => (dispatch, getState) => {
    updateErrorMessage(dispatch, undefined);
    KernelActions.fetchingKernel(dispatch, true);
    return Api.resampleInput(flow, kernelId)
        .then(response => {
            const kernelId = response.result;
            const warning = response.message;

            const curVersion = KernelSelectors.getKernelVersion(getState());

            if (version >= curVersion) {
                return KernelActions.updateKernel(dispatch, kernelId, version, warning);
            } else {
                return Api.deleteDiagnosticKernel(kernelId);
            }
        })
        .catch(error => {
            const message = getApiErrorMessage(error);
            updateErrorMessage(dispatch, message);
            KernelActions.fetchingKernel(dispatch, false);
            return Q.reject({ error: true, message: message });
        });
};

// Output Actions
export const updateOutputs = outputs => dispatch => {
    return dispatch({
        type: FLOW_UPDATE_OUTPUTS,
        payload: outputs
    });
};

export const newSinker = type => dispatch => {
    return dispatch({
        type: FLOW_NEW_SINKER,
        payload: type
    });
};

export const deleteSinker = index => dispatch => {
    return dispatch({
        type: FLOW_DELETE_SINKER,
        index: index
    });
};

export const updateSelectedSinkerIndex = index => dispatch => {
    return dispatch({
        type: FLOW_UPDATE_SELECTED_SINKER_INDEX,
        payload: index
    });
};

export const updateSinkerName = name => (dispatch, getState) => {
    updateSinker(
        dispatch,
        Selectors.getSelectedSinkerIndex(getState()),
        Object.assign({}, Selectors.getSelectedSinker(getState()), {
            id: name
        })
    );
};

// Cosmos DB Sinker
export const updateCosmosDbConnection = connection => (dispatch, getState) => {
    updateSinkerProperties(dispatch, getState, {
        connectionString: connection
    });
};

export const updateCosmosDbDatabase = database => (dispatch, getState) => {
    updateSinkerProperties(dispatch, getState, {
        db: database
    });
};

export const updateCosmosDbCollection = collection => (dispatch, getState) => {
    updateSinkerProperties(dispatch, getState, {
        collection: collection
    });
};

// Event Hub and Azure Blob Sinker
export const updateFormatType = type => (dispatch, getState) => {
    updateSinkerProperties(dispatch, getState, {
        format: type
    });
};

export const updateCompressionType = type => (dispatch, getState) => {
    updateSinkerProperties(dispatch, getState, {
        compressionType: type
    });
};

// Event Hub Sinker
export const updateEventHubConnection = connection => (dispatch, getState) => {
    updateSinkerProperties(dispatch, getState, {
        connectionString: connection
    });
};

// Azure Blob Sinker
export const updateBlobConnection = connection => (dispatch, getState) => {
    updateSinkerProperties(dispatch, getState, {
        connectionString: connection
    });
};

export const updateBlobContainerName = name => (dispatch, getState) => {
    updateSinkerProperties(dispatch, getState, {
        containerName: name
    });
};

export const updateBlobPrefix = prefix => (dispatch, getState) => {
    updateSinkerProperties(dispatch, getState, {
        blobPrefix: prefix
    });
};

export const updateBlobPartitionFormat = format => (dispatch, getState) => {
    updateSinkerProperties(dispatch, getState, {
        blobPartitionFormat: format
    });
};

function updateSinkerProperties(dispatch, getState, propertyMember) {
    updateSinker(
        dispatch,
        Selectors.getSelectedSinkerIndex(getState()),
        Object.assign({}, Selectors.getSelectedSinker(getState()), {
            properties: Object.assign({}, Selectors.getSelectedSinkerProperties(getState()), propertyMember)
        })
    );
}

function updateSinker(dispatch, index, sinker) {
    return dispatch({
        type: FLOW_UPDATE_SINKER,
        payload: sinker,
        index: index
    });
}

// Rule Actions
export const updateRules = rules => dispatch => {
    return dispatch({
        type: FLOW_UPDATE_RULES,
        payload: rules
    });
};

export const newRule = type => dispatch => {
    return dispatch({
        type: FLOW_NEW_RULE,
        payload: type
    });
};

export const deleteRule = index => dispatch => {
    return dispatch({
        type: FLOW_DELETE_RULE,
        index: index
    });
};

export const updateSelectedRuleIndex = index => dispatch => {
    return dispatch({
        type: FLOW_UPDATE_SELECTED_RULE_INDEX,
        payload: index
    });
};

export const updateRuleName = name => (dispatch, getState) => {
    updateRule(
        dispatch,
        Selectors.getSelectedRuleIndex(getState()),
        Object.assign({}, Selectors.getSelectedRule(getState()), {
            id: name
        })
    );
};

// Tag Rule
export const updateTagRuleSubType = type => (dispatch, getState) => {
    updateRuleProperties(dispatch, getState, {
        ruleType: type
    });
};

export const updateTagRuleDescription = description => (dispatch, getState) => {
    updateRule(
        dispatch,
        Selectors.getSelectedRuleIndex(getState()),
        Object.assign({}, Selectors.getSelectedRule(getState()), {
            // for Tag rules, we use the description as the rule name for display purposes
            id: description,
            // save description in properties bag
            properties: Object.assign({}, Selectors.getSelectedRuleProperties(getState()), { ruleDescription: description })
        })
    );
};

export const updateTagTag = tag => (dispatch, getState) => {
    updateRuleProperties(dispatch, getState, {
        tag: tag
    });
};

export const updateTagIsAlert = isAlert => (dispatch, getState) => {
    updateRuleProperties(dispatch, getState, {
        isAlert: isAlert
    });
};

export const updateTagSinks = sinks => (dispatch, getState) => {
    updateRuleProperties(dispatch, getState, {
        alertSinks: sinks
    });
};

export const updateTagSeverity = severity => (dispatch, getState) => {
    updateRuleProperties(dispatch, getState, {
        severity: severity
    });
};

export const updateTagConditions = conditions => (dispatch, getState) => {
    updateRuleProperties(dispatch, getState, {
        conditions: conditions
    });
};

export const updateTagAggregates = aggregates => (dispatch, getState) => {
    updateRuleProperties(dispatch, getState, {
        aggs: aggregates
    });
};

export const updateTagPivots = pivots => (dispatch, getState) => {
    updateRuleProperties(dispatch, getState, {
        pivots: pivots
    });
};

export const updateSchemaTableName = name => (dispatch, getState) => {
    updateRuleProperties(dispatch, getState, {
        schemaTableName: name
    });
};

function updateRuleProperties(dispatch, getState, propertyMember) {
    updateRule(
        dispatch,
        Selectors.getSelectedRuleIndex(getState()),
        Object.assign({}, Selectors.getSelectedRule(getState()), {
            properties: Object.assign({}, Selectors.getSelectedRuleProperties(getState()), propertyMember)
        })
    );
}

function updateRule(dispatch, index, rule) {
    return dispatch({
        type: FLOW_UPDATE_RULE,
        payload: rule,
        index: index
    });
}

// Scale Actions
export const updateNumExecutors = numExecutors => (dispatch, getState) => {
    updateScale(
        dispatch,
        Object.assign({}, Selectors.getFlowScale(getState()), {
            jobNumExecutors: numExecutors
        })
    );
};

export const updateExecutorMemory = executorMemory => (dispatch, getState) => {
    updateScale(
        dispatch,
        Object.assign({}, Selectors.getFlowScale(getState()), {
            jobExecutorMemory: executorMemory
        })
    );
};

export const updateDatabricksAutoScale = databricksAutoScale => (dispatch, getState) => {
    updateScale(
        dispatch,
        Object.assign({}, Selectors.getFlowScale(getState()), {
            jobDatabricksAutoScale: databricksAutoScale
        })
    );
};

export const updateDatabricksMinWorkers = databricksMinWorkers => (dispatch, getState) => {
    updateScale(
        dispatch,
        Object.assign({}, Selectors.getFlowScale(getState()), {
            jobDatabricksMinWorkers: databricksMinWorkers
        })
    );
};

export const updateDatabricksMaxWorkers = databricksMaxWorkers => (dispatch, getState) => {
    updateScale(
        dispatch,
        Object.assign({}, Selectors.getFlowScale(getState()), {
            jobDatabricksMaxWorkers: databricksMaxWorkers
        })
    );
};

function updateScale(dispatch, scale) {
    return dispatch({
        type: FLOW_UPDATE_SCALE,
        payload: scale
    });
}

// Message Actions
export function updateErrorMessage(dispatch, message) {
    return dispatch({
        type: FLOW_UPDATE_ERROR_MESSAGE,
        message: message
    });
}

export const updateWarningMessage = message => dispatch => {
    return dispatch({
        type: FLOW_UPDATE_WARNING_MESSAGE,
        warning: message
    });
};

const rejectWithMessage = (error, msg) =>
    Q.reject({
        error: true,
        message: msg + getApiErrorMessage(error)
    });

// Save and Delete Actions
export const saveFlow = flow => {
    return Api.saveFlow(Helpers.convertFlowToConfig(flow)).then(result => {
        const name = result.name;

        // generate job configurations for product
        return Api.generateProductConfigs(name)
            .then(result => {
                // restart all jobs associated with the product
                return Api.restartAllJobsForProduct(name).then(result => {
                    return name;
                });
            })
            .catch(error => {
                const message = getApiErrorMessage(error);
                return Q.reject({
                    error: true,
                    message: `There was an issue saving and starting the Flow. Please fix following error then save again and validate the job started correctly: ${message}`
                });
            });
    });
};

export const deleteFlow = flow => {
    return Api.deleteFlow(flow);
};

// OneBox action
export const updateOneBoxMode = enableLocalOneBox => dispatch => {
    return dispatch({
        type: FLOW_UPDATE_ONEBOX_MODE,
        payload: enableLocalOneBox
    });
};
