// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import * as Models from '../../flowModels';
import { Label } from 'office-ui-fabric-react';
import { DropdownMenuItemType } from 'office-ui-fabric-react/lib/Dropdown';
import { Colors } from 'datax-common';
import ConditionsPreview from './conditionsPreview';
import QueryBuilder from './queryBuilder';

export default class RuleConditionsSettings extends React.Component {
    constructor(props) {
        super(props);

        this.conjunctionOptions = Models.conjunctionTypes.map(type => {
            return {
                key: type.key,
                text: type.name,
                disabled: type.disabled
            };
        });

        this.aggregateOptions = Models.aggregateTypes.map(type => {
            return {
                key: type.key,
                text: type.name,
                disabled: type.disabled
            };
        });

        this.operatorOptionsForSimpleRule = [
            {
                key: 'numberheader',
                text: 'Numeric',
                itemType: DropdownMenuItemType.Header
            }
        ];

        Models.numberOperatorTypes.forEach(type => {
            this.operatorOptionsForSimpleRule.push({
                key: type.key,
                text: type.name,
                disabled: type.disabled
            });
        });

        this.operatorOptionsForAggregateRule = [...this.operatorOptionsForSimpleRule];

        this.operatorOptionsForSimpleRule.push({
            key: 'divider',
            text: '-',
            itemType: DropdownMenuItemType.Divider
        });

        this.operatorOptionsForSimpleRule.push({
            key: 'stringheader',
            text: 'Text',
            itemType: DropdownMenuItemType.Header
        });

        Models.stringOperatorTypes.forEach(type => {
            this.operatorOptionsForSimpleRule.push({
                key: type.key,
                text: type.name,
                disabled: type.disabled
            });
        });
    }

    render() {
        const supportAggregate = this.props.rule.properties.ruleType === Models.ruleSubTypeEnum.AggregateRule;

        let fieldOptions = [];
        if (
            this.props.rule.properties.schemaTableName in this.props.tableToSchemaMap &&
            this.props.tableToSchemaMap[this.props.rule.properties.schemaTableName].columns
        ) {
            const columns = this.props.tableToSchemaMap[this.props.rule.properties.schemaTableName].columns;
            fieldOptions = columns.map(value => {
                return {
                    key: value,
                    text: value
                };
            });
        }

        return (
            <div>
                <Label className="ms-font-m ms-fontWeight-semibold" style={titleStyle}>
                    Conditions
                </Label>

                <div style={sectionContainerStyle}>
                    <ConditionsPreview query={this.props.rule.properties.conditions} supportAggregate={supportAggregate} />

                    <QueryBuilder
                        query={this.props.rule.properties.conditions}
                        supportAggregate={supportAggregate}
                        conjunctionOptions={this.conjunctionOptions}
                        aggregateOptions={this.aggregateOptions}
                        fieldOptions={fieldOptions}
                        operatorOptionsForSimpleRule={this.operatorOptionsForSimpleRule}
                        operatorOptionsForAggregateRule={this.operatorOptionsForAggregateRule}
                        fieldPlaceHolder="column name"
                        valuePlaceHolder="expected value"
                        onUpdateQuery={this.props.onUpdateTagConditions}
                    />
                </div>
            </div>
        );
    }
}

// Props
RuleConditionsSettings.propTypes = {
    rule: PropTypes.object.isRequired,
    tableToSchemaMap: PropTypes.object.isRequired,

    onUpdateTagConditions: PropTypes.func.isRequired
};

// Styles
const titleStyle = {
    color: Colors.customBlueDark,
    marginBottom: 5
};

const sectionContainerStyle = {
    display: 'flex',
    flexDirection: 'column',
    flexWrap: 'wrap',
    backgroundColor: Colors.neutralLighter,
    border: `1px solid ${Colors.neutralQuaternaryAlt}`,
    marginBottom: 20
};
