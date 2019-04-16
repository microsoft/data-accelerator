// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import * as Models from '../../flowModels';
import RuleGeneralSettings from './ruleGeneralSettings';
import RuleConditionsSettings from './ruleConditionsSettings';
import RuleAggregateColumnSettings from './ruleAggregateColumnSettings';
import RulePivotSettings from './rulePivotSettings';
import RuleAlertSettings from './ruleAlertSettings';

export default class TagRuleSettings extends React.Component {
    constructor(props) {
        super(props);
    }

    render() {
        return (
            <div style={rootStyle}>
                {this.renderGeneralSettings()}
                {this.renderConditionsSettings()}
                {this.renderAggregateColumnSettings()}
                {this.renderPivotSettings()}
                {this.renderAlertSettings()}
            </div>
        );
    }

    renderGeneralSettings() {
        return (
            <RuleGeneralSettings
                rule={this.props.rule}
                tableToSchemaMap={this.props.tableToSchemaMap}
                onUpdateRuleSubType={this.props.onUpdateTagRuleSubType}
                onUpdateRuleDescription={this.props.onUpdateTagRuleDescription}
                onUpdateTag={this.props.onUpdateTagTag}
                onUpdateSchemaTableName={this.props.onUpdateSchemaTableName}
            />
        );
    }

    renderConditionsSettings() {
        return (
            <RuleConditionsSettings
                rule={this.props.rule}
                tableToSchemaMap={this.props.tableToSchemaMap}
                onUpdateTagConditions={this.props.onUpdateTagConditions}
            />
        );
    }

    renderAggregateColumnSettings() {
        if (this.props.rule.properties.ruleType === Models.ruleSubTypeEnum.AggregateRule) {
            return (
                <RuleAggregateColumnSettings
                    rule={this.props.rule}
                    tableToSchemaMap={this.props.tableToSchemaMap}
                    onUpdateAggregates={this.props.onUpdateTagAggregates}
                />
            );
        } else {
            return null;
        }
    }

    renderPivotSettings() {
        if (this.props.rule.properties.ruleType === Models.ruleSubTypeEnum.AggregateRule) {
            return (
                <RulePivotSettings
                    rule={this.props.rule}
                    tableToSchemaMap={this.props.tableToSchemaMap}
                    onUpdatePivots={this.props.onUpdateTagPivots}
                />
            );
        } else {
            return null;
        }
    }

    renderAlertSettings() {
        return (
            <RuleAlertSettings
                rule={this.props.rule}
                sinkers={this.props.sinkers}
                onUpdateIsAlert={this.props.onUpdateTagIsAlert}
                onUpdateSinks={this.props.onUpdateTagSinks}
                onUpdateSeverity={this.props.onUpdateTagSeverity}
            />
        );
    }
}

// Props
TagRuleSettings.propTypes = {
    rule: PropTypes.object.isRequired,
    sinkers: PropTypes.array.isRequired,
    tableToSchemaMap: PropTypes.object.isRequired,

    onUpdateTagRuleSubType: PropTypes.func.isRequired,
    onUpdateTagRuleDescription: PropTypes.func.isRequired,
    onUpdateTagTag: PropTypes.func.isRequired,
    onUpdateTagIsAlert: PropTypes.func.isRequired,
    onUpdateTagSinks: PropTypes.func.isRequired,
    onUpdateTagSeverity: PropTypes.func.isRequired,
    onUpdateTagConditions: PropTypes.func.isRequired,
    onUpdateTagAggregates: PropTypes.func.isRequired,
    onUpdateTagPivots: PropTypes.func.isRequired,
    onUpdateSchemaTableName: PropTypes.func.isRequired
};

// Styles
const rootStyle = {
    paddingLeft: 20,
    paddingRight: 20,
    paddingBottom: 20
};
