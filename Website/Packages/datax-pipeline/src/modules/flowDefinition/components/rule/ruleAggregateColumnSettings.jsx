// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import * as Models from '../../flowModels';
import { Label, TextField, DefaultButton, ComboBox } from 'office-ui-fabric-react';
import { Colors, IconButtonStyles } from 'datax-common';

const useDropdownControl = true;

export default class RuleAggregateColumnSettings extends React.Component {
    constructor(props) {
        super(props);
    }

    render() {
        const aggregates = this.props.rule.properties.aggs.map((item, index) => {
            return this.renderAggregateColumn(item, index, this.props.rule.properties.aggs.length);
        });

        return (
            <div>
                <Label className="ms-font-m ms-fontWeight-semibold" style={titleStyle}>
                    Additional Aggregate Columns to Include
                </Label>

                <div style={aggregateSectionContainerStyle}>
                    {aggregates}
                    {this.renderAddAggregateColumnButton()}
                </div>
            </div>
        );
    }

    renderAggregateColumn(item, index, length) {
        let comma =
            index < length - 1 ? (
                <Label className="ms-fontSize-xl ms-fontWeight-semibold" style={commaStyle}>
                    ,
                </Label>
            ) : null;

        return (
            <div key={`aggregate_column_${index}`} style={aggregateColumnStyle}>
                {this.renderAggregateOfAggregateColumnDropdown(item, index)}
                {this.renderColumnOfAggregateColumnDropdown(item, index)}
                {this.renderDeleteAggregateColumnButton(index)}
                {comma}
            </div>
        );
    }

    renderAggregateOfAggregateColumnDropdown(item, index) {
        const options = Models.aggregateTypes
            .filter(type => type.key !== Models.aggregateTypeEnum.none)
            .map(type => {
                return {
                    key: type.key,
                    text: type.name,
                    disabled: type.disabled
                };
            });

        return (
            <div style={aggregateDropdownStyle}>
                <ComboBox
                    className="ms-font-m"
                    options={options}
                    selectedKey={item.aggregate}
                    onChange={(event, selection) => this.onUpdateAggregateOfAggregateColumn(selection.key, index)}
                />
            </div>
        );
    }

    renderColumnOfAggregateColumnDropdown(item, index) {
        if (useDropdownControl) {
            let options = [];
            if (
                this.props.rule.properties.schemaTableName in this.props.tableToSchemaMap &&
                this.props.tableToSchemaMap[this.props.rule.properties.schemaTableName].columns
            ) {
                const columns = this.props.tableToSchemaMap[this.props.rule.properties.schemaTableName].columns;
                options = columns.map(value => {
                    return {
                        key: value,
                        text: value
                    };
                });
            }

            return (
                <div style={aggregateColumnSectionStyle}>
                    <ComboBox
                        className="ms-font-m"
                        options={options}
                        placeholder="column name"
                        selectedKey={item.column}
                        onChange={(event, selection) => this.onUpdateColumnOfAggregateColumn(selection.key, index)}
                    />
                </div>
            );
        } else {
            return (
                <div style={aggregateColumnSectionStyle}>
                    <TextField
                        className="ms-font-m"
                        spellCheck={false}
                        placeholder="column name"
                        value={item.column}
                        onChange={(event, value) => this.onUpdateColumnOfAggregateColumn(value, index)}
                    />
                </div>
            );
        }
    }

    renderAddAggregateColumnButton() {
        return (
            <DefaultButton
                key="newaggregatecolumn"
                className="rule-settings-button"
                style={addButtonStyle}
                title="Add new aggregate column"
                onClick={() => this.onAddAggregateColumn()}
            >
                <i style={IconButtonStyles.greenStyle} className="ms-Icon ms-Icon--Add" />
                Add Column
            </DefaultButton>
        );
    }

    renderDeleteAggregateColumnButton(index) {
        return (
            <DefaultButton
                key="deleteaggregatecolumn"
                className="rule-settings-delete-button"
                style={deleteButtonStyle}
                title="Delete aggregate column"
                onClick={() => this.onDeleteAggregateColumn(index)}
            >
                <i style={IconButtonStyles.neutralStyle} className="ms-Icon ms-Icon--Delete" />
            </DefaultButton>
        );
    }

    onAddAggregateColumn() {
        const aggregates = [...this.props.rule.properties.aggs, Models.getDefaultAggregateColumn()];
        this.props.onUpdateAggregates(aggregates);
    }

    onDeleteAggregateColumn(index) {
        const aggregates = [...this.props.rule.properties.aggs];
        aggregates.splice(index, 1);
        this.props.onUpdateAggregates(aggregates);
    }

    onUpdateAggregateOfAggregateColumn(value, index) {
        const aggregates = [...this.props.rule.properties.aggs];
        aggregates[index].aggregate = value;
        this.props.onUpdateAggregates(aggregates);
    }

    onUpdateColumnOfAggregateColumn(value, index) {
        const aggregates = [...this.props.rule.properties.aggs];
        aggregates[index].column = value;
        this.props.onUpdateAggregates(aggregates);
    }
}

// Props
RuleAggregateColumnSettings.propTypes = {
    rule: PropTypes.object.isRequired,
    tableToSchemaMap: PropTypes.object.isRequired,

    onUpdateAggregates: PropTypes.func.isRequired
};

// Styles
const titleStyle = {
    color: Colors.customBlueDark,
    marginBottom: 5
};

const aggregateSectionContainerStyle = {
    display: 'flex',
    flexDirection: 'row',
    flexWrap: 'wrap',
    backgroundColor: Colors.neutralLighter,
    border: `1px solid ${Colors.neutralQuaternaryAlt}`,
    paddingLeft: 15,
    paddingRight: 0,
    paddingTop: 25,
    paddingBottom: 10,
    marginBottom: 20
};

const aggregateColumnStyle = {
    display: 'flex',
    flexDirection: 'row'
};

const aggregateDropdownStyle = {
    paddingBottom: 15,
    paddingRight: 15,
    width: 110,
    minWidth: 110
};

const aggregateColumnSectionStyle = {
    paddingBottom: 15,
    paddingRight: 0,
    width: 200,
    minWidth: 200
};

const commaStyle = {
    color: Colors.customBlueDark,
    paddingRight: 15,
    marginTop: -3
};

const addButtonStyle = {
    marginBottom: 15
};

const deleteButtonStyle = {
    marginBottom: 15
};
