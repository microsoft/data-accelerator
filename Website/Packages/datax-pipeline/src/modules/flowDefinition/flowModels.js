// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
export const inputModeEnum = {
    streaming: 'streaming',
    batching: 'batching'
};

export const batchIntervalTypeEnum = {
    day: 'day',
    hour: 'hour',
    min: 'min'
};

export const inputTypeEnum = {
    events: 'events',
    iothub: 'iothub',
    kafka: 'kafka',
    kafkaeventhub: 'kafkaeventhub',
    blob: 'blob',
    local: 'local'
};

export const inputCompressionTypeEnum = {
    none: 'none',
    gzip: 'gzip'
};

export const inputFormatTypeEnum = {
    json: 'json',
    parquet: 'parquet'
};

export const watermarkUnitEnum = {
    second: 'second',
    minute: 'minute',
    hour: 'hour'
};

export const referenceDataTypeEnum = {
    csv: 'csv'
};

export const csvDelimiterEnum = {
    comma: ',',
    tab: '\t'
};

export const functionMethodTypeEnum = {
    get: 'get',
    post: 'post'
};

export const functionTypeEnum = {
    azureFunction: 'azureFunction',
    udf: 'jarUDF',
    udaf: 'jarUDAF'
};

export const sinkerTypeEnum = {
    cosmosdb: 'cosmosdb',
    eventHub: 'eventHub',
    blob: 'blob',
    metric: 'metric',
    local: 'local',
    sql: 'sqlServer'
};

export const batchTypeEnum = {
    recurring: 'recurring',
    oneTime: 'oneTime'
};

export const sinkerCompressionTypeEnum = {
    none: 'none',
    gzip: 'gzip'
};

export const sinkerFormatTypeEnum = {
    json: 'json'
};

export const sinkerSqlWriteModeEnum = {
    append: 'append',
    overwrite: 'overwrite',
    ignore: 'ignore',
    errorifexists: 'errorIfExists'
};

export const sinkerSqlWriteModes = [
    {
        key: sinkerSqlWriteModeEnum.append,
        name: 'Append',
        disabled: false
    },
    {
        key: sinkerSqlWriteModeEnum.overwrite,
        name: 'Overwrite',
        disabled: false
    },
    {
        key: sinkerSqlWriteModeEnum.ignore,
        name: 'Ignore',
        disabled: false
    },
    {
        key: sinkerSqlWriteModeEnum.errorIfExists,
        name: 'Error if exists',
        disabled: false
    }
];

export const ruleTypeEnum = {
    tag: 'tag'
};

export const ruleSubTypeEnum = {
    SimpleRule: 'SimpleRule',
    AggregateRule: 'AggregateRule'
};

export const aggregateTypeEnum = {
    MIN: 'MIN',
    MAX: 'MAX',
    AVG: 'AVG',
    SUM: 'SUM',
    COUNT: 'COUNT',
    DCOUNT: 'DCOUNT',
    none: 'none'
};

export const conditionTypeEnum = {
    group: 'group',
    condition: 'condition'
};

export const operatorTypeEnum = {
    equal: 'equal',
    notEqual: 'notEqual',
    greaterThan: 'greater',
    lessThan: 'lessThan',
    greaterThanOrEqual: 'greaterThanOrEqual',
    lessThanOrEqual: 'lessThanOrEqual',
    stringEqual: 'stringEqual',
    stringNotEqual: 'stringNotEqual',
    contains: 'contains',
    notContains: 'notContains',
    startsWith: 'startsWith',
    endsWith: 'endsWith'
};

export const conjunctionTypeEnum = {
    or: 'or',
    and: 'and'
};

export const severityTypeEnum = {
    Critical: 'Critical',
    Medium: 'Medium',
    Low: 'Low'
};

export const inputModes = [
    {
        key: inputModeEnum.streaming,
        name: 'Streaming',
        disabled: false
    },
    {
        key: inputModeEnum.batching,
        name: 'Batching',
        disabled: false
    }
];

export const inputTypes = [
    {
        key: inputTypeEnum.events,
        name: 'Event Hub',
        disabled: false
    },
    {
        key: inputTypeEnum.iothub,
        name: 'IoT Hub',
        disabled: false
    },
    {
        key: inputTypeEnum.kafka,
        name: 'Kafka',
        disabled: false
    },
    {
        key: inputTypeEnum.kafkaeventhub,
        name: 'Kafka (Event Hub)',
        disabled: false
    },
    {
        key: inputTypeEnum.local,
        name: 'Local',
        disabled: false
    }
];

export const inputTypesBatching = [
    {
        key: inputTypeEnum.blob,
        name: 'Azure Blob',
        disabled: false
    }
];

export const inputCompressionTypes = [
    {
        key: inputCompressionTypeEnum.none,
        name: 'None',
        disabled: false
    }
];

export const inputFormatTypes = [
    {
        key: inputFormatTypeEnum.json,
        name: 'JSON',
        disabled: false
    }
];

export const watermarkUnits = [
    {
        key: watermarkUnitEnum.second,
        name: 'Seconds',
        disabled: false
    },
    {
        key: watermarkUnitEnum.minute,
        name: 'Minutes',
        disabled: false
    },
    {
        key: watermarkUnitEnum.hour,
        name: 'Hours',
        disabled: false
    }
];

export const referenceDataTypes = [
    {
        key: referenceDataTypeEnum.csv,
        name: 'CSV/TSV File',
        disabled: false
    }
];

export const csvDelimiterTypes = [
    {
        key: csvDelimiterEnum.comma,
        name: 'Comma',
        disabled: false
    },
    {
        key: csvDelimiterEnum.tab,
        name: 'Tab',
        disabled: false
    }
];

export const functionTypes = [
    {
        key: functionTypeEnum.udf,
        name: 'UDF',
        disabled: false
    },
    {
        key: functionTypeEnum.udaf,
        name: 'UDAF',
        disabled: false
    },
    {
        key: functionTypeEnum.azureFunction,
        name: 'Azure Function',
        disabled: false
    }
];

export const functionMethodTypes = [
    {
        key: functionMethodTypeEnum.get,
        name: 'Get',
        disabled: false
    },
    {
        key: functionMethodTypeEnum.post,
        name: 'Post',
        disabled: false
    }
];

export const outputSinkerTypes = [
    {
        key: sinkerTypeEnum.blob,
        name: 'Azure Blob',
        disabled: false
    },
    {
        key: sinkerTypeEnum.cosmosdb,
        name: 'Cosmos DB',
        disabled: false
    },
    {
        key: sinkerTypeEnum.eventHub,
        name: 'Event Hub',
        disabled: false
    },
    {
        key: sinkerTypeEnum.sql,
        name: 'Azure SQL Database',
        disabled: false
    },
    {
        key: sinkerTypeEnum.local,
        name: 'Local',
        disabled: false
    }
];

export const batchTypes = [
    {
        key: batchTypeEnum.recurring,
        name: 'Recurring',
        disabled: false
    },
    {
        key: batchTypeEnum.oneTime,
        name: 'One Time',
        disabled: false
    }
];

export const batchIntervalTypes = [
    {
        key: batchIntervalTypeEnum.day,
        name: 'Day',
        disabled: false
    },
    {
        key: batchIntervalTypeEnum.hour,
        name: 'Hour',
        disabled: false
    },
    {
        key: batchIntervalTypeEnum.min,
        name: 'Min',
        disabled: false
    }
];

export const sinkerCompressionTypes = [
    {
        key: sinkerCompressionTypeEnum.none,
        name: 'None',
        disabled: false
    },
    {
        key: sinkerCompressionTypeEnum.gzip,
        name: 'GZIP',
        disabled: false
    }
];

export const sinkerFormatTypes = [
    {
        key: sinkerFormatTypeEnum.json,
        name: 'JSON',
        disabled: false
    }
];

export const ruleTypes = [
    {
        key: ruleTypeEnum.tag,
        name: 'Tag Rule',
        disabled: false
    }
];

export const ruleSubTypes = [
    {
        key: ruleSubTypeEnum.SimpleRule,
        name: 'Simple',
        disabled: false
    },
    {
        key: ruleSubTypeEnum.AggregateRule,
        name: 'Aggregate',
        disabled: false
    }
];

export const aggregateTypes = [
    {
        key: aggregateTypeEnum.MIN,
        name: 'MIN',
        disabled: false
    },
    {
        key: aggregateTypeEnum.MAX,
        name: 'MAX',
        disabled: false
    },
    {
        key: aggregateTypeEnum.AVG,
        name: 'AVG',
        disabled: false
    },
    {
        key: aggregateTypeEnum.SUM,
        name: 'SUM',
        disabled: false
    },
    {
        key: aggregateTypeEnum.COUNT,
        name: 'COUNT',
        disabled: false
    },
    {
        key: aggregateTypeEnum.DCOUNT,
        name: 'DCOUNT',
        disabled: false
    },
    {
        key: aggregateTypeEnum.none,
        name: '(none)',
        disabled: false
    }
];

export const aggregateDistinctKeyword = 'DISTINCT';

export const conditionTypes = [
    {
        key: conditionTypeEnum.condition,
        name: 'Condition',
        disabled: false
    },
    {
        key: conditionTypeEnum.group,
        name: 'Group',
        disabled: false
    }
];

export const numberOperatorTypes = [
    {
        key: operatorTypeEnum.equal,
        name: '=',
        disabled: false
    },
    {
        key: operatorTypeEnum.notEqual,
        name: '<>',
        disabled: false
    },
    {
        key: operatorTypeEnum.greaterThan,
        name: '>',
        disabled: false
    },
    {
        key: operatorTypeEnum.lessThan,
        name: '<',
        disabled: false
    },
    {
        key: operatorTypeEnum.greaterThanOrEqual,
        name: '>=',
        disabled: false
    },
    {
        key: operatorTypeEnum.lessThanOrEqual,
        name: '<=',
        disabled: false
    }
];

export const stringOperatorTypes = [
    {
        key: operatorTypeEnum.stringEqual,
        name: 'Equal',
        disabled: false
    },
    {
        key: operatorTypeEnum.stringNotEqual,
        name: 'Not Equal',
        disabled: false
    },
    {
        key: operatorTypeEnum.contains,
        name: 'Contains',
        disabled: false
    },
    {
        key: operatorTypeEnum.notContains,
        name: 'Not Contains',
        disabled: false
    },
    {
        key: operatorTypeEnum.startsWith,
        name: 'Starts With',
        disabled: false
    },
    {
        key: operatorTypeEnum.endsWith,
        name: 'Ends With',
        disabled: false
    }
];

export const conjunctionTypes = [
    {
        key: conjunctionTypeEnum.or,
        name: 'OR',
        disabled: false
    },
    {
        key: conjunctionTypeEnum.and,
        name: 'AND',
        disabled: false
    }
];

export const severityTypes = [
    {
        key: severityTypeEnum.Critical,
        name: 'Critical',
        disabled: false
    },
    {
        key: severityTypeEnum.Medium,
        name: 'Medium',
        disabled: false
    },
    {
        key: severityTypeEnum.Low,
        name: 'Low',
        disabled: false
    }
];

export function getDefaultBatchInputSettings() {
    return {
        type: inputTypeEnum.blob,
        properties: {
            connection: '',
            path: '',
            formatType: inputFormatTypeEnum.json,
            compressionType: inputCompressionTypeEnum.none
        }
    };
}

export function getDefaultReferenceDataSettings(type) {
    if (type === referenceDataTypeEnum.csv) {
        return {
            id: '',
            type: type,
            properties: {
                path: '',
                delimiter: csvDelimiterEnum.comma,
                header: true
            }
        };
    } else {
        return {
            id: '',
            type: type,
            properties: {}
        };
    }
}

export function getDefaultFunctionSettings(type) {
    if (type === functionTypeEnum.udf || type === functionTypeEnum.udaf) {
        return {
            id: '',
            type: type,
            properties: {
                path: '',
                class: '',
                libs: []
            }
        };
    } else if (type === functionTypeEnum.azureFunction) {
        return {
            id: '',
            type: type,
            properties: {
                serviceEndpoint: '',
                api: '',
                code: '',
                methodType: 'get',
                params: []
            }
        };
    } else {
        return {
            id: '',
            type: type,
            properties: {}
        };
    }
}

export const metricSinkerName = 'Metrics';
export const metricSinkerTypeName = 'Metrics';

export function getMetricSinker() {
    return {
        id: metricSinkerName,
        type: sinkerTypeEnum.metric,
        properties: {}
    };
}

export function getDefaultBatchSettings(type) {
    if (type === batchTypeEnum.oneTime) {
        return {
            id: '',
            type: type,
            disabled: false,
            properties: {
                interval: '1',
                intervalType: 'day',
                delay: '0',
                delayType: 'day',
                window: '1',
                windowType: 'day',
                startTime: '',
                endTime: '',
                lastProcessedTime: ''
            }
        };
    } else {
        return {
            id: '',
            type: type,
            disabled: false,
            properties: {
                interval: '1',
                intervalType: 'day',
                delay: '0',
                delayType: 'day',
                window: '1',
                windowType: 'day',
                startTime: new Date(),
                endTime: '',
                lastProcessedTime: ''
            }
        };
    }
}

export function getDefaultSinkerSettings(type, owner) {
    if (type === sinkerTypeEnum.cosmosdb) {
        return {
            id: '',
            type: type,
            properties: {
                connectionString: '',
                db: '',
                collection: ''
            }
        };
    } else if (type === sinkerTypeEnum.eventHub) {
        return {
            id: '',
            type: type,
            properties: {
                connectionString: '',
                format: sinkerFormatTypeEnum.json,
                compressionType: sinkerCompressionTypeEnum.gzip
            }
        };
    } else if (type === sinkerTypeEnum.blob) {
        return {
            id: '',
            type: type,
            properties: {
                connectionString: '',
                containerName: '',
                blobPrefix: '',
                blobPartitionFormat: '',
                format: sinkerFormatTypeEnum.json,
                compressionType: sinkerCompressionTypeEnum.gzip
            }
        };
    } else if (type === sinkerTypeEnum.local) {
        return {
            id: '',
            type: type,
            properties: {
                connectionString: '',
                containerName: '',
                blobPrefix: '',
                blobPartitionFormat: '',
                format: sinkerFormatTypeEnum.json,
                compressionType: sinkerCompressionTypeEnum.none
            }
        };
    } else if (type === sinkerTypeEnum.sql) {
        return {
            id: '',
            type: type,
            properties: {
                connectionString: '',
                tableName: '',
                writeMode: sinkerSqlWriteModeEnum.append,
                useBulkInsert: false
            }
        };
    } else {
        return {
            id: '',
            type: type,
            properties: {}
        };
    }
}

export const DefaultSchemaTableName = 'DataXProcessedInput';

export function getDefaultAggregateColumn() {
    return {
        aggregate: aggregateTypeEnum.AVG,
        column: ''
    };
}

export function getDefaultConditionSettings() {
    return {
        type: conditionTypeEnum.condition,
        conjunction: conjunctionTypeEnum.or,
        aggregate: aggregateTypeEnum.AVG,
        field: '',
        operator: operatorTypeEnum.equal,
        value: ''
    };
}

export function getDefaultGroupSettings() {
    return {
        type: conditionTypeEnum.group,
        conjunction: conjunctionTypeEnum.or,
        conditions: [getDefaultConditionSettings()]
    };
}

export function getDefaultRuleSettings(type) {
    if (type === ruleTypeEnum.tag) {
        return {
            id: '',
            type: type,
            properties: {
                // general rule settings
                productId: '', // assigned by NPOT
                ruleType: ruleSubTypeEnum.SimpleRule,
                ruleId: '', // assigned by NPOT
                ruleDescription: '',
                condition: '', // fill out when we convert from Flow to Config
                tagName: 'Tag',
                tag: '',
                aggs: [],
                pivots: [],
                // alert settings
                isAlert: false,
                severity: severityTypeEnum.Critical,
                alertSinks: [],
                outputTemplate: '',
                // website UI settings
                schemaTableName: DefaultSchemaTableName,
                conditions: getDefaultGroupSettings()
            }
        };
    } else {
        return {
            id: '',
            type: type,
            properties: {}
        };
    }
}

export function getDefaultName() {
    // Generate a 5 digit random value
    // We need a default display name to generate a valid flowId. This is needed for SchemaInferenceService and InteractiveQueryService features
    const rand = Math.floor(Math.random() * 90000) + 10000;
    return `test${rand}`;
}

export function getDefaultInput(enableLocalOneBox) {
    if (enableLocalOneBox) {
        return defaultInputLocal;
    } else {
        return defaultInput;
    }
}

export const defaultSchema = `{
  "type": "struct",
  "fields": [
    {
      "name": "name_of_simple_type_field_to_extract_as_your_first_column",
      "type": "double",
      "nullable": true,
      "metadata": {}
    },
    {
      "name": "name_of_simple_type_field_to_extract_as_your_second_column",
      "type": "string",
      "nullable": true,
      "metadata": {}
    },
    {
      "name": "name_of_property_bag_type_field_to_extract_as_your_third_column",
      "type": {
        "type": "map",
        "keyType": "string",
        "valueType": "string",
        "valueContainsNull": true
      },
      "nullable": true,
      "metadata": {}
    }
  ]
}
`;

export const defaultSchemaLocal = `{
    "type": "struct",
    "fields": [
      {
        "name": "temperature",
        "type": "double",
        "nullable": false,
        "metadata": {
          "minValue": 5.1,
          "maxValue": 100.1
        }
      },
      {
        "name": "eventTime",
        "type": "long",
        "nullable": false,
        "metadata": { "useCurrentTimeMillis": true }
      }
    ]
  }
  `;

export function getDefaultNormalizationSnippet(inputMode) {
    if (inputMode === inputModeEnum.batching) {
        return defaultBatchNormalizationSnippet;
    } else {
        return defaultNormalizationSnippet;
    }
}

export const defaultNormalizationSnippet = `SystemProperties AS _SystemProperties
Properties AS _Properties
Raw.*`;

export const defaultBatchNormalizationSnippet = `Raw.*`;

// Default Flow settings
export const defaultInput = {
    type: inputTypeEnum.events,
    mode: inputModeEnum.streaming,
    properties: {
        inputEventhubName: '',
        inputEventhubConnection: '',
        inputSubscriptionId: '',
        inputResourceGroup: '',
        windowDuration: '30',
        timestampColumn: '',
        watermarkValue: '0',
        watermarkUnit: watermarkUnitEnum.second,
        maxRate: '1000',
        inputSchemaFile: defaultSchema,
        showNormalizationSnippet: false,
        normalizationSnippet: defaultNormalizationSnippet
    }
};

export const defaultInputLocal = {
    type: inputTypeEnum.local,
    mode: inputModeEnum.streaming,
    properties: {
        inputEventhubName: '',
        inputEventhubConnection: '',
        inputSubscriptionId: '',
        inputResourceGroup: '',
        windowDuration: '30',
        timestampColumn: '',
        watermarkValue: '0',
        watermarkUnit: watermarkUnitEnum.second,
        maxRate: '100',
        inputSchemaFile: defaultSchemaLocal,
        showNormalizationSnippet: false,
        normalizationSnippet: defaultNormalizationSnippet
    }
};
