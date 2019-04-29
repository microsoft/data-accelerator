// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import { createSelector } from 'reselect';
import * as Helpers from './flowHelpers';
import * as Models from './flowModels';

// Settings - Flow
export const getFlow = state => state.flow;

// Settings - Message
export const getWarningMessage = createSelector(
    getFlow,
    flow => flow.warningMessage
);

export const getErrorMessage = createSelector(
    getFlow,
    flow => flow.errorMessage
);

// Settings - Info
export const getFlowDisplayName = createSelector(
    getFlow,
    flow => flow.displayName
);

// Settings - Input
export const getFlowInput = createSelector(
    getFlow,
    flow => flow.input
);

export const getFlowInputProperties = createSelector(
    getFlowInput,
    input => input.properties
);

// Settings - Reference Data
export const getFlowReferenceData = createSelector(
    getFlow,
    flow => flow.referenceData
);

export const getSelectedReferenceDataIndex = createSelector(
    getFlow,
    flow => flow.selectedReferenceDataIndex
);

export const getSelectedReferenceData = createSelector(
    getFlowReferenceData,
    getSelectedReferenceDataIndex,
    selectedReferenceData
);

export const getSelectedReferenceDataProperties = createSelector(
    getSelectedReferenceData,
    referenceData => referenceData.properties
);

function selectedReferenceData(referenceData, selectedIndex) {
    return selectedIndex !== undefined && selectedIndex < referenceData.length ? referenceData[selectedIndex] : undefined;
}

// Settings - Functions
export const getFlowFunctions = createSelector(
    getFlow,
    flow => flow.functions
);

export const getSelectedFunctionIndex = createSelector(
    getFlow,
    flow => flow.selectedFunctionIndex
);

export const getSelectedFunction = createSelector(
    getFlowFunctions,
    getSelectedFunctionIndex,
    selectedFunction
);

export const getSelectedFunctionProperties = createSelector(
    getSelectedFunction,
    functionItem => functionItem.properties
);

function selectedFunction(functions, selectedIndex) {
    return selectedIndex !== undefined && selectedIndex < functions.length ? functions[selectedIndex] : undefined;
}

// Settings - Query
export const getFlowQuery = createSelector(
    getFlow,
    flow => flow.query
);

// Settings - Scale
export const getFlowScale = createSelector(
    getFlow,
    flow => flow.scale
);

// Settings - Outputs
export const getFlowOutputs = createSelector(
    getFlow,
    flow => flow.outputs
);

export const getSelectedSinkerIndex = createSelector(
    getFlow,
    flow => flow.selectedSinkerIndex
);

export const getSelectedSinker = createSelector(
    getFlowOutputs,
    getSelectedSinkerIndex,
    selectedSinker
);

export const getSelectedSinkerProperties = createSelector(
    getSelectedSinker,
    sinker => sinker.properties
);

function selectedSinker(sinkers, selectedIndex) {
    return selectedIndex !== undefined && selectedIndex < sinkers.length ? sinkers[selectedIndex] : undefined;
}

// Settings - Output Templates
export const getFlowOutputTemplates = createSelector(
    getFlow,
    flow => flow.outputTemplates
);

export const getSelectedOutputTemplateIndex = createSelector(
    getFlow,
    flow => flow.selectedOutputTemplateIndex
);

export const getSelectedOutputTemplate = createSelector(
    getFlowOutputTemplates,
    getSelectedOutputTemplateIndex,
    selectedOutputTemplate
);

function selectedOutputTemplate(rules, selectedIndex) {
    return selectedIndex !== undefined && selectedIndex < rules.length ? rules[selectedIndex] : undefined;
}

// Settings - Rules
export const getFlowRules = createSelector(
    getFlow,
    flow => flow.rules
);

export const getSelectedRuleIndex = createSelector(
    getFlow,
    flow => flow.selectedRuleIndex
);

export const getSelectedRule = createSelector(
    getFlowRules,
    getSelectedRuleIndex,
    selectedRule
);

export const getSelectedRuleProperties = createSelector(
    getSelectedRule,
    rule => rule.properties
);

function selectedRule(rules, selectedIndex) {
    return selectedIndex !== undefined && selectedIndex < rules.length ? rules[selectedIndex] : undefined;
}

// Settings - OneBox
export const getEnableLocalOneBox = createSelector(
    getFlow,
    flow => flow.enableLocalOneBox
);

// Validation - Info
export const validateFlowInfo = createSelector(
    getFlowDisplayName,
    validateInfo
);

function validateInfo(displayName) {
    return displayName && displayName.trim() !== '';
}

// Validation - Input
export const validateFlowInput = createSelector(
    getFlowInput,
    validateInput
);

function validateInput(input) {
    let validations = [];
    validations.push(input && input.properties);

    if (input.mode === Models.inputModeEnum.streaming) {
        if (input.type === Models.inputTypeEnum.events || input.type === Models.inputTypeEnum.eventforkafka) {
            validations.push(input.properties.inputEventhubConnection.trim() !== '');
            validations.push(Helpers.isValidNumberAboveZero(input.properties.windowDuration));
            validations.push(
                input.properties.watermarkValue.trim() !== '' && Helpers.isValidNumberAboveOrEqualZero(input.properties.watermarkValue)
            );
            validations.push(Helpers.isValidNumberAboveZero(input.properties.maxRate));
            validations.push(Helpers.isValidJson(input.properties.inputSchemaFile));
        } else if (input.type === Models.inputTypeEnum.iothub) {
            validations.push(input.properties.inputEventhubName.trim() !== '');
            validations.push(input.properties.inputEventhubConnection.trim() !== '');
            validations.push(Helpers.isValidNumberAboveZero(input.properties.windowDuration));
            validations.push(
                input.properties.watermarkValue.trim() !== '' && Helpers.isValidNumberAboveOrEqualZero(input.properties.watermarkValue)
            );
            validations.push(Helpers.isValidNumberAboveZero(input.properties.maxRate));
            validations.push(Helpers.isValidJson(input.properties.inputSchemaFile));
        } else if (input.type === Models.inputTypeEnum.local) {
            validations.push(Helpers.isValidNumberAboveZero(input.properties.windowDuration));
            validations.push(
                input.properties.watermarkValue.trim() !== '' && Helpers.isValidNumberAboveOrEqualZero(input.properties.watermarkValue)
            );
            validations.push(Helpers.isValidNumberAboveZero(input.properties.maxRate));
            validations.push(Helpers.isValidJson(input.properties.inputSchemaFile));
        } else {
            validation.push(false);
        }
    } else {
        // future support
        validation.push(false);
    }
    return validations.every(value => value);
}

// Validation - Reference Data
export const validateFlowReferenceData = createSelector(
    getFlowReferenceData,
    validateReferenceData
);

function validateReferenceData(referenceData) {
    return referenceData && referenceData.every(isReferenceDataSettingsComplete);
}

function isReferenceDataSettingsComplete(referenceData) {
    let validations = [];
    validations.push(referenceData && referenceData.properties);
    validations.push(Helpers.isNumberAndStringOnly(referenceData.id));

    switch (referenceData.type) {
        case Models.referenceDataTypeEnum.csv:
            validations.push(referenceData.properties.path && referenceData.properties.path.trim() !== '');
            break;

        default:
            // if unknown type, return validation failure so newly onboarded reference data will look into what needs to be validated
            validations.push(false);
            break;
    }
    return validations.every(value => value);
}

// Validation - Functions
export const validateFlowFunctions = createSelector(
    getFlowFunctions,
    validateFunctions
);

function validateFunctions(functions) {
    return functions && functions.every(isFunctionSettingsComplete);
}

function isFunctionSettingsComplete(functionItem) {
    let validations = [];
    validations.push(functionItem && functionItem.properties);
    validations.push(Helpers.isNumberAndStringOnly(functionItem.id));

    switch (functionItem.type) {
        case Models.functionTypeEnum.udf:
        case Models.functionTypeEnum.udaf:
            validations.push(functionItem.properties.path && functionItem.properties.path.trim() !== '');
            validations.push(functionItem.properties.class && functionItem.properties.class.trim() !== '');

            if (functionItem.properties.libs && functionItem.properties.libs.length > 0) {
                functionItem.properties.libs.forEach(libPath => {
                    validations.push(libPath.trim() !== '');
                });
            }
            break;
        case Models.functionTypeEnum.azureFunction:
            validations.push(functionItem.properties.serviceEndpoint && functionItem.properties.serviceEndpoint.trim() !== '');
            validations.push(functionItem.properties.api && functionItem.properties.api.trim() !== '');
            validations.push(functionItem.properties.code && functionItem.properties.code.trim() !== '');
            validations.push(functionItem.properties.methodType && functionItem.properties.methodType.trim() !== '');

            if (functionItem.properties.params && functionItem.properties.params.length > 0) {
                functionItem.properties.params.forEach(param => {
                    validations.push(param.trim() !== '');
                });
            }
            break;
        default:
            // if unknown type, return validation failure so newly onboarded function will look into what needs to be validated
            validations.push(false);
            break;
    }
    return validations.every(value => value);
}

// Validation - Query
export const validateFlowQuery = createSelector(
    getFlowQuery,
    validateQuery
);

function validateQuery(query) {
    //removing validation; codegen will add OUTPUTs for alerts. Blank query is valid.
    return query || query.trim() === '';
}

// Validation - Outputs
export const validateFlowOutputs = createSelector(
    getFlowOutputs,
    validateOutputs
);

function validateOutputs(outputs) {
    return outputs && outputs.length > 0 && outputs.every(isSinkerSettingsComplete);
}

function isSinkerSettingsComplete(sinker) {
    let validations = [];
    validations.push(sinker && sinker.properties);
    validations.push(Helpers.isNumberAndStringOnly(sinker.id));

    switch (sinker.type) {
        case Models.sinkerTypeEnum.cosmosdb:
            validations.push(sinker.properties.connectionString && sinker.properties.connectionString.trim() !== '');
            validations.push(sinker.properties.db && Helpers.isNumberAndStringOnly(sinker.properties.db));
            validations.push(sinker.properties.collection && Helpers.isNumberAndStringOnly(sinker.properties.collection));
            break;

        case Models.sinkerTypeEnum.eventHub:
            validations.push(sinker.properties.connectionString && sinker.properties.connectionString.trim() !== '');
            break;

        case Models.sinkerTypeEnum.blob:
            validations.push(sinker.properties.connectionString && sinker.properties.connectionString.trim() !== '');
            validations.push(sinker.properties.containerName && Helpers.isNumberAndStringOnly(sinker.properties.containerName));
            validations.push(sinker.properties.blobPrefix && sinker.properties.blobPrefix.trim() !== '');
            validations.push(sinker.properties.blobPartitionFormat && sinker.properties.blobPartitionFormat.trim() !== '');
            break;

        case Models.sinkerTypeEnum.metric:
            // no additional validation needed, this is a system provided sinker type
            break;

        case Models.sinkerTypeEnum.local:
            validations.push(sinker.properties.connectionString && sinker.properties.connectionString.trim() !== '');
            break;

        default:
            // if unknown type, return validation failure so newly onboarded sinker will look into what needs to be validated
            validations.push(false);
            break;
    }
    return validations.every(value => value);
}

// Validation - Output Templates
export const validateFlowOutputTemplates = createSelector(
    getFlowOutputTemplates,
    validateOutputTemplates
);

function validateOutputTemplates(outputTemplates) {
    return outputTemplates && outputTemplates.every(isOutputTemplateSettingsComplete);
}

function isOutputTemplateSettingsComplete(outputTemplate) {
    let validations = [];
    validations.push(outputTemplate);
    validations.push(outputTemplate.id && outputTemplate.id.trim() !== '');
    validations.push(outputTemplate.template && outputTemplate.template.trim() !== '');

    return validations.every(value => value);
}

// Validation - Rules
export const validateFlowRules = createSelector(
    getFlowRules,
    validateRules
);

function validateRules(rules) {
    return rules && rules.every(isRuleSettingsComplete);
}

function isRuleSettingsComplete(rule) {
    let validations = [];
    validations.push(rule && rule.properties);
    validations.push(rule.id && rule.id.trim() !== '');

    switch (rule.type) {
        case Models.ruleTypeEnum.tag:
            validations.push(rule.properties.ruleDescription && rule.properties.ruleDescription.trim() !== '');
            validations.push(rule.properties.isAlert ? rule.properties.alertSinks.length > 0 : true);
            validations.push(Helpers.isConditionsValid(rule.properties.conditions, rule.properties.ruleType, false, false));
            validations.push(isAggregatesComplete(rule));
            validations.push(isPivotsComplete(rule));
            break;

        default:
            // if unknown type, return validation failure so newly onboarded rule will look into what needs to be validated
            validations.push(false);
            break;
    }
    return validations.every(value => value);
}

function isAggregatesComplete(rule) {
    if (rule.properties.ruleType === Models.ruleSubTypeEnum.AggregateRule && rule.properties.aggs.length > 0) {
        return rule.properties.aggs.every(aggregate => aggregate.column !== '');
    } else {
        return true;
    }
}

function isPivotsComplete(rule) {
    if (rule.properties.ruleType === Models.ruleSubTypeEnum.AggregateRule && rule.properties.pivots.length > 0) {
        return rule.properties.pivots.every(pivot => pivot !== '');
    } else {
        return true;
    }
}

// Validation - Scale
export const validateFlowScale = createSelector(
    getFlowScale,
    validateScale
);

function validateScale(scale) {
    return scale && Helpers.isValidNumberAboveZero(scale.jobNumExecutors) && Helpers.isValidNumberAboveZero(scale.jobExecutorMemory);
}

// Validation -  Flow
export const validateFlow = createSelector(
    validateFlowInfo,
    validateFlowInput,
    validateFlowFunctions,
    validateFlowQuery,
    validateFlowOutputs,
    validateFlowOutputTemplates,
    validateFlowRules,
    validateFlowScale,
    (...selectors) => selectors.every(value => value)
);
