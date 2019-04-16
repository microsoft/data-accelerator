// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import * as Models from '../../flowModels';
import { TextField, DefaultButton, Dropdown } from 'office-ui-fabric-react';
import { Colors, IconButtonStyles } from 'datax-common';

const maxGroupCycleDepth = 4;

const groupCycleStyles = {
    0: { backgroundColor: Colors.neutralLighter, border: `1px solid ${Colors.neutralQuaternaryAlt}` },
    1: { backgroundColor: Colors.neutralLight, border: `1px solid ${Colors.neutralQuaternaryAlt}` },
    2: { backgroundColor: Colors.neutralQuaternaryAlt, border: `1px solid ${Colors.neutralTertiaryAlt}` },
    3: { backgroundColor: Colors.neutralLight, border: `1px solid ${Colors.neutralTertiaryAlt}` }
};

export default class QueryBuilder extends React.Component {
    constructor(props) {
        super(props);
    }

    render() {
        return <div style={rootStyle}>{this.renderGroup(this.props.query, 0, undefined, 0, true)}</div>;
    }

    renderGroup(group, groupIndex, parentGroup, depth, isRoot) {
        let conditions = group.conditions.map((condition, index) => {
            if (condition.type === Models.conditionTypeEnum.group) {
                return this.renderGroup(condition, index, group, depth + 1, false);
            } else {
                return this.renderCondition(condition, index, group.conditions.length, group, depth);
            }
        });

        let style = Object.assign({}, groupStyle, {
            backgroundColor: groupCycleStyles[depth % maxGroupCycleDepth].backgroundColor
        });

        if (!isRoot) {
            style.marginBottom = 15;
            style.border = groupCycleStyles[depth % maxGroupCycleDepth].border;
        }

        return (
            <div key={`Group_${depth}_${groupIndex}`}>
                {this.renderGroupConjunctionDropdown(group, groupIndex)}

                <div style={style}>
                    <div style={groupToolbarStyle}>
                        {this.renderAddConditionButton(group)}
                        {!isRoot && this.renderDeleteGroupButton(parentGroup, groupIndex)}
                    </div>

                    <div style={groupContentStyle}>{conditions}</div>
                </div>
            </div>
        );
    }

    renderCondition(condition, index, length, parentGroup, depth) {
        return (
            <div key={`Condition_${depth}_${index}`} style={conditionStyle}>
                {this.renderConditionConjunctionDropdown(condition, index, length)}
                {this.renderAggregateDropdown(condition)}
                {this.renderFieldDropdown(condition)}
                {this.renderOperatorDropdown(condition)}
                {this.renderValueControl(condition)}
                {this.renderDeleteConditionButton(parentGroup, index)}
            </div>
        );
    }

    renderGroupConjunctionDropdown(group, index) {
        const isFirstCondition = index === 0;

        if (isFirstCondition) {
            return null;
        } else {
            return (
                <div style={groupConjunctionDropdownStyle}>
                    <Dropdown
                        className="ms-font-m"
                        options={this.props.conjunctionOptions}
                        selectedKey={group.conjunction}
                        onChange={(event, selection) => this.onUpdateConjunction(group, selection.key)}
                    />
                </div>
            );
        }
    }

    renderConditionConjunctionDropdown(condition, index, length) {
        const isOnlyCondition = length === 1;
        const isFirstCondition = index === 0;

        if (isOnlyCondition) {
            return null;
        } else if (!isOnlyCondition && isFirstCondition) {
            return <div style={conditionConjunctionDropdownStyle} />;
        } else {
            return (
                <div style={conditionConjunctionDropdownStyle}>
                    <Dropdown
                        className="ms-font-m"
                        options={this.props.conjunctionOptions}
                        selectedKey={condition.conjunction}
                        onChange={(event, selection) => this.onUpdateConjunction(condition, selection.key)}
                    />
                </div>
            );
        }
    }

    renderAggregateDropdown(condition) {
        if (this.props.supportAggregate) {
            return (
                <div style={aggregateDropdownStyle}>
                    <Dropdown
                        className="ms-font-m"
                        options={this.props.aggregateOptions}
                        selectedKey={condition.aggregate}
                        onChange={(event, selection) => this.onUpdateAggregate(condition, selection.key)}
                    />
                </div>
            );
        } else {
            return null;
        }
    }

    renderFieldDropdown(condition) {
        return (
            <div style={fieldDropdownStyle}>
                <Dropdown
                    className="ms-font-m"
                    options={this.props.fieldOptions}
                    placeholder={this.props.fieldPlaceHolder}
                    selectedKey={condition.field}
                    onChange={(event, selection) => this.onUpdateField(condition, selection.key)}
                />
            </div>
        );
    }

    renderOperatorDropdown(condition) {
        const operatorOptions =
            this.props.supportAggregate && condition.aggregate !== Models.aggregateTypeEnum.none
                ? this.props.operatorOptionsForAggregateRule
                : this.props.operatorOptionsForSimpleRule;

        return (
            <div style={operatorDropdownStyle}>
                <Dropdown
                    className="ms-font-m"
                    options={operatorOptions}
                    selectedKey={condition.operator}
                    onChange={(event, selection) => this.onUpdateOperator(condition, selection.key)}
                />
            </div>
        );
    }

    renderValueControl(condition) {
        return (
            <div style={valueControlStyle}>
                <TextField
                    className="ms-font-m"
                    spellCheck={false}
                    placeholder={this.props.valuePlaceHolder}
                    value={condition.value}
                    onChange={(event, value) => this.onUpdateValue(condition, value)}
                />
            </div>
        );
    }

    renderAddConditionButton(parentGroup) {
        const menuItems = Models.conditionTypes.map(conditionType => {
            return Object.assign({}, conditionType, {
                onClick: () => this.onAddCondition(parentGroup, conditionType.key)
            });
        });

        return (
            <DefaultButton
                key="newcondition"
                className="rule-settings-button"
                title="Add new condition or group"
                menuProps={{ items: menuItems }}
            >
                <i style={IconButtonStyles.greenStyle} className="ms-Icon ms-Icon--Add" />
                Add
            </DefaultButton>
        );
    }

    renderDeleteGroupButton(parentGroup, index) {
        return (
            <DefaultButton
                key="deletegroup"
                className="rule-settings-delete-button"
                title="Delete group"
                onClick={() => this.onDeleteConditionGroup(parentGroup, index)}
            >
                <i style={IconButtonStyles.neutralStyle} className="ms-Icon ms-Icon--Delete" />
            </DefaultButton>
        );
    }

    renderDeleteConditionButton(parentGroup, index) {
        return (
            <DefaultButton
                key="deletecondition"
                className="rule-settings-delete-button"
                style={conditionButtonStyle}
                title="Delete condition"
                onClick={() => this.onDeleteConditionGroup(parentGroup, index)}
            >
                <i style={IconButtonStyles.neutralStyle} className="ms-Icon ms-Icon--Delete" />
            </DefaultButton>
        );
    }

    onUpdateConjunction(conditionGroup, value) {
        conditionGroup.conjunction = value;
        this.updateQuery();
    }

    onUpdateAggregate(condition, value) {
        condition.aggregate = value;
        this.updateQuery();
    }

    onUpdateField(condition, value) {
        condition.field = value;
        this.updateQuery();
    }

    onUpdateOperator(condition, value) {
        condition.operator = value;
        this.updateQuery();
    }

    onUpdateValue(condition, value) {
        condition.value = value;
        this.updateQuery();
    }

    onAddCondition(parentGroup, conditionType) {
        if (conditionType === Models.conditionTypeEnum.condition) {
            parentGroup.conditions.push(Models.getDefaultConditionSettings());
        } else if (conditionType === Models.conditionTypeEnum.group) {
            parentGroup.conditions.push(Models.getDefaultGroupSettings());
        } else {
            alert('unknown condition type');
        }

        this.updateQuery();
    }

    onDeleteConditionGroup(parentGroup, index) {
        parentGroup.conditions.splice(index, 1);
        this.updateQuery();
    }

    updateQuery() {
        this.props.onUpdateQuery(this.props.query);
    }
}

// Props
QueryBuilder.propTypes = {
    query: PropTypes.object.isRequired,
    supportAggregate: PropTypes.bool.isRequired,
    conjunctionOptions: PropTypes.array.isRequired,
    aggregateOptions: PropTypes.array.isRequired,
    fieldOptions: PropTypes.array.isRequired,
    operatorOptionsForSimpleRule: PropTypes.array.isRequired,
    operatorOptionsForAggregateRule: PropTypes.array.isRequired,
    fieldPlaceHolder: PropTypes.string.isRequired,
    valuePlaceHolder: PropTypes.string.isRequired,

    onUpdateQuery: PropTypes.func.isRequired
};

// Styles
const rootStyle = {
    minHeight: 100
};

const groupStyle = {
    display: 'flex',
    flexDirection: 'column'
};

const conditionStyle = {
    display: 'flex',
    flexDirection: 'row',
    flexWrap: 'wrap'
};

const groupToolbarStyle = {
    display: 'flex',
    flexDirection: 'row',
    paddingBottom: 10
};

const groupContentStyle = {
    paddingLeft: 15,
    paddingRight: 15
};

const groupConjunctionDropdownStyle = {
    paddingBottom: 15,
    width: 80,
    minWidth: 80
};

const conditionConjunctionDropdownStyle = {
    paddingBottom: 15,
    paddingRight: 15,
    width: 80,
    minWidth: 80
};

const aggregateDropdownStyle = {
    paddingBottom: 15,
    paddingRight: 15,
    width: 130,
    minWidth: 130
};

const fieldDropdownStyle = {
    flex: 1,
    paddingBottom: 15,
    paddingRight: 15,
    width: 200,
    minWidth: 200
};

const operatorDropdownStyle = {
    paddingBottom: 15,
    paddingRight: 15,
    width: 130,
    minWidth: 130
};

const valueControlStyle = {
    flex: 2,
    paddingBottom: 15,
    width: 200,
    minWidth: 200
};

const conditionButtonStyle = {
    marginBottom: 15
};
