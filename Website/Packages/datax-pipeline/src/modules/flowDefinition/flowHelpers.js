// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import * as Models from './flowModels';
import * as Api from '../../common/api';

export function isValidNumberAboveOrEqualZero(value) {
    const number = Number(value);
    const isNumber = !isNaN(number);
    const isValid = isNumber && number >= 0;
    return isValid;
}

export function isValidNumber(value) {
    const number = Number(value);
    return !isNaN(number);
}

export function isValidJson(jsonString) {
    try {
        JSON.parse(jsonString);
        return true;
    } catch (error) {
        return false;
    }
}

export function isNumberAndStringOnly(value) {
    // contains lowercase letters, uppercase letters, and numbers
    const validWordRegex = /^\w+$/;
    return validWordRegex.test(value);
}

export function isConditionsValid(conditions, ruleType, ignoreEmptyFieldAndValue, ignoreEmptyGroup) {
    return validateConditions(conditions, ruleType, ignoreEmptyFieldAndValue, ignoreEmptyGroup) === undefined;
}

export function validateConditions(conditions, ruleType, ignoreEmptyFieldAndValue, ignoreEmptyGroup) {
    let conditionValidationFuncs = [],
        groupValidationFuncs = [];

    if (ruleType === Models.ruleSubTypeEnum.AggregateRule) {
        conditionValidationFuncs.push(condition => {
            if (condition.aggregate !== Models.aggregateTypeEnum.none && !isNumberOperator(condition.operator)) {
                throw 'Text operators cannot be used with Aggregate conditions';
            }
        });
    }

    if (!ignoreEmptyFieldAndValue) {
        conditionValidationFuncs.push(condition => {
            if (!condition.field) throw 'All conditions need to have column name specified';
            if (!condition.value) throw 'All conditions need to have a value specified';
        });
    }

    if (!ignoreEmptyGroup) {
        groupValidationFuncs.push(group => {
            if (group.conditions.length === 0) throw 'All groups need to have at least 1 condition';
        });
    }

    conditionValidationFuncs.push(condition => {
        if (isNumberOperator(condition.operator) && condition.value !== '' && !isValidNumber(condition.value))
            throw 'Value field must be a number when a numeric operator is used';
    });

    try {
        validateGroup(conditions, groupValidationFuncs, conditionValidationFuncs);
        return undefined;
    } catch (message) {
        return message;
    }
}

function validateGroup(group, groupValidationFuncs, conditionValidationFuncs) {
    groupValidationFuncs.forEach(func => func(group));

    group.conditions.forEach(condition => {
        if (condition.type === Models.conditionTypeEnum.group) {
            validateGroup(condition, groupValidationFuncs, conditionValidationFuncs);
        } else {
            validateCondition(condition, conditionValidationFuncs);
        }
    });
}

function validateCondition(condition, conditionValidationFuncs) {
    conditionValidationFuncs.forEach(func => func(condition));
}

export function formatRuleConditionsToString(conditions, supportAggregate) {
    if (conditions) {
        return formatGroupToString(conditions, 0, supportAggregate).slice(1, -1);
    } else {
        return '';
    }
}

function formatGroupToString(group, groupIndex, supportAggregate) {
    let text = '';
    if (groupIndex > 0) {
        text += formatConjunction(group.conjunction);
    }

    text += '(';
    group.conditions.forEach((condition, index) => {
        if (condition.type === Models.conditionTypeEnum.group) {
            text += formatGroupToString(condition, index, supportAggregate);
        } else {
            text += formatConditionToString(condition, index, supportAggregate);
        }
    });
    return text + ')';
}

function formatConditionToString(condition, conditionIndex, supportAggregate) {
    let text = '';
    if (conditionIndex > 0) {
        text += formatConjunction(condition.conjunction);
    }

    if (supportAggregate) {
        text += formatAggregateField(condition.aggregate, condition.field);
    } else {
        text += condition.field;
    }
    return text + ` ${formatOperator(condition.operator)} ${formatValue(condition.operator, condition.value)}`;
}

export function formatAggregateField(aggregate, field) {
    if (aggregate === Models.aggregateTypeEnum.DCOUNT) {
        return `${Models.aggregateTypeEnum.COUNT}(${Models.aggregateDistinctKeyword} ${field})`;
    } else if (aggregate === Models.aggregateTypeEnum.none) {
        return field;
    } else {
        return `${aggregate}(${field})`;
    }
}

export function formatValue(operator, value) {
    if (isNumberOperator(operator)) {
        return value;
    } else {
        const replaceAll = (text, search, replace) => {
            return text.replace(new RegExp(search, 'g'), replace);
        };

        value = replaceAll(value, "'", "''");

        switch (operator) {
            case Models.operatorTypeEnum.contains:
            case Models.operatorTypeEnum.notContains:
                return `'%${value}%'`;
            case Models.operatorTypeEnum.startsWith:
                return `'${value}%'`;
            case Models.operatorTypeEnum.endsWith:
                return `'%${value}'`;
            default:
                return `'${value}'`;
        }
    }
}

export function formatConjunction(conjunction) {
    return ` ${conjunction.toUpperCase()} `;
}

export function formatOperator(operator) {
    switch (operator) {
        case Models.operatorTypeEnum.equal:
            return '=';
        case Models.operatorTypeEnum.notEqual:
            return '<>';
        case Models.operatorTypeEnum.greaterThan:
            return '>';
        case Models.operatorTypeEnum.lessThan:
            return '<';
        case Models.operatorTypeEnum.greaterThanOrEqual:
            return '>=';
        case Models.operatorTypeEnum.lessThanOrEqual:
            return '<=';
        case Models.operatorTypeEnum.stringEqual:
            return '=';
        case Models.operatorTypeEnum.stringNotEqual:
            return '<>';
        case Models.operatorTypeEnum.contains:
        case Models.operatorTypeEnum.startsWith:
        case Models.operatorTypeEnum.endsWith:
            return 'LIKE';
        case Models.operatorTypeEnum.notContains:
            return 'NOT LIKE';
        default:
            return '';
    }
}

export function isNumberOperator(operator) {
    switch (operator) {
        case Models.operatorTypeEnum.equal:
        case Models.operatorTypeEnum.notEqual:
        case Models.operatorTypeEnum.greaterThan:
        case Models.operatorTypeEnum.lessThan:
        case Models.operatorTypeEnum.greaterThanOrEqual:
        case Models.operatorTypeEnum.lessThanOrEqual:
            return true;
        case Models.operatorTypeEnum.stringEqual:
        case Models.operatorTypeEnum.stringNotEqual:
        case Models.operatorTypeEnum.contains:
        case Models.operatorTypeEnum.notContains:
        case Models.operatorTypeEnum.startsWith:
        case Models.operatorTypeEnum.endsWith:
        default:
            return false;
    }
}

function convertFlowToConfigAggregates(supportAggregate, conditions, aggregates) {
    if (supportAggregate) {
        let uniqueAggregateSet = new Set();

        // auto add all aggregates from conditions which is what user specifies in Query Builder UI
        const addAggregatesOfGroup = group => {
            group.conditions.forEach(condition => {
                if (condition.type === Models.conditionTypeEnum.group) {
                    addAggregatesOfGroup(condition);
                } else {
                    if (condition.aggregate !== Models.aggregateTypeEnum.none) {
                        uniqueAggregateSet.add(formatAggregateField(condition.aggregate, condition.field));
                    }
                }
            });
        };
        addAggregatesOfGroup(conditions);

        // add additional aggregates to include that is specified by user in Aggregate UI
        aggregates.forEach(aggregate => {
            uniqueAggregateSet.add(formatAggregateField(aggregate.aggregate, aggregate.column));
        });

        // this list of aggregates contains only unique items
        return [...uniqueAggregateSet];
    } else {
        return [];
    }
}

function convertConfigToFlowAggregates(supportAggregate, conditions, aggregates) {
    if (supportAggregate && aggregates && aggregates.length > 0) {
        let aggregatesFromConditionsSet = new Set();

        // track all aggregates from conditions which is what user specifies in Query Builder UI
        const addAggregatesOfGroup = group => {
            group.conditions.forEach(condition => {
                if (condition.type === Models.conditionTypeEnum.group) {
                    addAggregatesOfGroup(condition);
                } else {
                    if (condition.aggregate !== Models.aggregateTypeEnum.none) {
                        aggregatesFromConditionsSet.add(formatAggregateField(condition.aggregate, condition.field));
                    }
                }
            });
        };
        addAggregatesOfGroup(conditions);

        // aggregates list we return should only contain any additional aggregates user specifies.
        // we do not want the ones from the query builder that we previously auto inserted when saving the config.
        let additionalAggregates = [];
        aggregates.forEach(aggregate => {
            const regexAggregateAndColumn = /^(\S+)\((.+)\)$/;
            let matches = aggregate.match(regexAggregateAndColumn);
            let aggregateCandidate = { aggregate: matches[1], column: matches[2] };

            // check if aggregate is distinct count, if so parse out the column name from COUNT(DISTINCT column_name)
            const regexDistinctCountColumn = /^DISTINCT (.+)$/;
            if (
                aggregateCandidate.aggregate === Models.aggregateTypeEnum.COUNT &&
                regexDistinctCountColumn.test(aggregateCandidate.column)
            ) {
                matches = aggregateCandidate.column.match(regexDistinctCountColumn);
                aggregateCandidate = { aggregate: Models.aggregateTypeEnum.DCOUNT, column: matches[1] };
            }

            const aggregateCandidateString = formatAggregateField(aggregateCandidate.aggregate, aggregateCandidate.column);

            if (!aggregatesFromConditionsSet.has(aggregateCandidateString)) {
                additionalAggregates.push(aggregateCandidate);
            }
        });
        return additionalAggregates;
    } else {
        return [];
    }
}

function convertFlowToConfigPivots(supportAggregate, conditions, pivots) {
    if (supportAggregate) {
        let uniquePivotSet = new Set();

        // auto add all none aka (no aggregates) from conditions which is what user specifies in Query Builder UI
        const addPivotsOfGroup = group => {
            group.conditions.forEach(condition => {
                if (condition.type === Models.conditionTypeEnum.group) {
                    addPivotsOfGroup(condition);
                } else {
                    if (condition.aggregate === Models.aggregateTypeEnum.none) {
                        uniquePivotSet.add(condition.field);
                    }
                }
            });
        };
        addPivotsOfGroup(conditions);

        // add additional pivots to include that is specified by user in Group By UI
        pivots.forEach(pivot => {
            uniquePivotSet.add(pivot);
        });

        // this list of pivots contains only unique items
        return [...uniquePivotSet];
    } else {
        return [];
    }
}

function convertConfigToFlowPivots(supportAggregate, conditions, pivots) {
    if (supportAggregate && pivots && pivots.length > 0) {
        let pivotsFromConditionsSet = new Set();

        // track all none aka (no aggregates) from conditions which is what user specifies in Query Builder UI
        const addPivotsOfGroup = group => {
            group.conditions.forEach(condition => {
                if (condition.type === Models.conditionTypeEnum.group) {
                    addPivotsOfGroup(condition);
                } else {
                    if (condition.aggregate === Models.aggregateTypeEnum.none) {
                        pivotsFromConditionsSet.add(condition.field);
                    }
                }
            });
        };
        addPivotsOfGroup(conditions);

        // pivots list we return should only contain any additional pivots user specifies.
        // we do not want the ones from the query builder that we previously auto inserted when saving the config.
        let additionalPivots = [];
        pivots.forEach(pivot => {
            if (!pivotsFromConditionsSet.has(pivot)) {
                additionalPivots.push(pivot);
            }
        });
        return additionalPivots;
    } else {
        return [];
    }
}

export function convertFlowToConfigRules(rules) {
    let configRules = [];

    rules.forEach(rule => {
        const supportAggregate = rule.properties.ruleType === Models.ruleSubTypeEnum.AggregateRule;

        if (rule.type === Models.ruleTypeEnum.tag) {
            configRules.push({
                id: rule.id,
                type: rule.type,
                properties: {
                    $productId: rule.properties.productId,
                    $ruleType: rule.properties.ruleType,
                    $ruleId: rule.properties.ruleId,
                    $ruleDescription: rule.properties.ruleDescription,
                    $condition: formatRuleConditionsToString(rule.properties.conditions, supportAggregate),
                    $tagName: rule.properties.tagName,
                    $tag: rule.properties.tag,
                    $aggs: convertFlowToConfigAggregates(supportAggregate, rule.properties.conditions, rule.properties.aggs),
                    $pivots: convertFlowToConfigPivots(supportAggregate, rule.properties.conditions, rule.properties.pivots),
                    // alert settings
                    $isAlert: rule.properties.isAlert,
                    $severity: rule.properties.severity,
                    $alertSinks: rule.properties.alertSinks,
                    $outputTemplate: rule.properties.outputTemplate,
                    // website UI settings
                    schemaTableName: rule.properties.schemaTableName,
                    conditions: rule.properties.conditions
                }
            });
        }
    });

    return configRules;
}

export function convertConfigToFlowRules(rules) {
    let flowRules = [];

    if (rules) {
        rules.forEach(rule => {
            if (rule.type === Models.ruleTypeEnum.tag) {
                const supportAggregate = rule.properties.$ruleType === Models.ruleSubTypeEnum.AggregateRule;
                const conditions = rule.properties.conditions ? rule.properties.conditions : Models.getDefaultGroupSettings();

                flowRules.push({
                    id: rule.id,
                    type: rule.type,
                    properties: {
                        productId: rule.properties.$productId,
                        ruleType: rule.properties.$ruleType,
                        ruleId: rule.properties.$ruleId,
                        ruleDescription: rule.properties.$ruleDescription,
                        condition: rule.properties.$condition,
                        tagName: rule.properties.$tagName,
                        tag: rule.properties.$tag,
                        aggs: convertConfigToFlowAggregates(supportAggregate, conditions, rule.properties.$aggs),
                        pivots: convertConfigToFlowPivots(supportAggregate, conditions, rule.properties.$pivots),
                        // alert settings
                        isAlert: rule.properties.$isAlert,
                        severity: rule.properties.$severity,
                        alertSinks: rule.properties.$alertSinks,
                        outputTemplate: rule.properties.$outputTemplate,
                        // website UI settings
                        schemaTableName: rule.properties.schemaTableName,
                        conditions: conditions
                    }
                });
            }
        });
    }

    return flowRules;
}

export function isMetricSinker(sinker) {
    return sinker.type === Models.sinkerTypeEnum.metric;
}

export function convertFlowToConfig(flow, query) {
    let referenceData = [...flow.referenceData];
    let functions = [...flow.functions];
    let sinkers = [...flow.outputs];
    let outputTemplates = [...flow.outputTemplates];
    let rules = [...flow.rules];
    let batchList = flow.batchList ? [...flow.batchList] : [];
    let batchInputs = flow.batchInputs ? [...flow.batchInputs] : [Models.getDefaultBatchInputSettings()];

    // sort by name
    referenceData.sort((a, b) => a.id.localeCompare(b.id));
    functions.sort((a, b) => a.id.localeCompare(b.id));
    sinkers.sort((a, b) => a.id.localeCompare(b.id));
    outputTemplates.sort((a, b) => a.id.localeCompare(b.id));
    rules.sort((a, b) => a.id.localeCompare(b.id));
    batchList.sort((b, a) => a.type.localeCompare(b.type));

    // return product config
    return {
        name: flow.name,
        flowId: flow.flowId,
        displayName: flow.displayName.trim(),
        owner: flow.owner,
        databricksToken: flow.databricksToken,
        input: Object.assign({}, flow.input, { referenceData: flow.referenceData, batch: batchInputs }),
        process: {
            timestampColumn: flow.input.properties.timestampColumn,
            watermark: `${flow.input.properties.watermarkValue} ${flow.input.properties.watermarkUnit}`,
            functions: functions,
            queries: [query],
            jobconfig: flow.scale
        },
        outputs: sinkers,
        outputTemplates: outputTemplates,
        rules: convertFlowToConfigRules(rules),
        batchList: batchList
    };
}

export function convertConfigToFlow(config) {
    let input = config.input;

    // adapt default value for input properties defined before subscription id and resource group existed
    if (!input.properties.inputSubscriptionId) {
        input.properties.inputSubscriptionId = '';
    }

    if (!input.properties.inputResourceGroup) {
        input.properties.inputResourceGroup = '';
    }

    // return flow understood by our website
    let flow = {
        name: config.name,
        flowId: config.flowId,
        displayName: config.displayName,
        owner: config.owner,
        databricksToken: config.databricksToken,
        input: input,
        batchInputs: input.batch ? input.batch : [Models.getDefaultBatchInputSettings()],
        batchList: config.batchList ? config.batchList : [],
        referenceData: input.referenceData ? input.referenceData : [],
        functions: config.process.functions ? config.process.functions : [],
        query: config.process.queries[0],
        scale: config.process.jobconfig,
        outputs: config.outputs,
        outputTemplates: config.outputTemplates ? config.outputTemplates : [],
        rules: convertConfigToFlowRules(config.rules)
    };

    return flow;
}
/** This is the contract between the package that is dependent on datax-query package. For example, datax-pipeline package needs to pass in queryMetadata object which contains all the required parameters as needed by various apis in the datax-query package*/
export function convertFlowToQueryMetadata(flow, query) {
    // return query metadata
    let QueryMetadata = {
        name: flow.name,
        databricksToken: flow.databricksToken,
        displayName: flow.displayName,
        userName: flow.owner,
        refData: flow.referenceData,
        inputSchema: flow.input.properties.inputSchemaFile,
        normalizationSnippet: flow.input.properties.normalizationSnippet,
        outputTemplates: flow.outputTemplates,
        functions: flow.functions,
        rules: convertFlowToConfigRules(flow.rules),
        eventhubConnection: flow.input.properties.inputEventhubConnection,
        inputResourceGroup: flow.input.properties.inputResourceGroup,
        eventhubNames: flow.input.properties.inputEventhubName,
        inputType: flow.input.type,
        seconds: flow.resamplingInputDuration,
        query: query,
        inputSubscriptionId: flow.input.properties.inputSubscriptionId
    };
    return QueryMetadata;
}
