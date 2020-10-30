// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import { Label, TextField, DefaultButton, ComboBox } from 'office-ui-fabric-react';
import { Colors, IconButtonStyles } from 'datax-common';

const useDropdownControl = true;

export default class RulePivotSettings extends React.Component {
    constructor(props) {
        super(props);
    }

    render() {
        const pivots = this.props.rule.properties.pivots.map((item, index) => {
            return this.renderPivotColumn(item, index, this.props.rule.properties.pivots.length);
        });

        return (
            <div>
                <Label className="ms-font-m ms-fontWeight-semibold" style={titleStyle}>
                    Group By Columns
                </Label>

                <div style={aggregateSectionContainerStyle}>
                    {pivots}
                    {this.renderAddPivotColumnButton()}
                </div>
            </div>
        );
    }

    renderPivotColumn(item, index, length) {
        let comma =
            index < length - 1 ? (
                <Label className="ms-fontSize-xl ms-fontWeight-semibold" style={commaStyle}>
                    ,
                </Label>
            ) : null;

        return (
            <div key={`pivot_column_${index}`} style={pivotColumnStyle}>
                {this.renderPivotColumnDropdown(item, index)}
                {this.renderDeletePivotColumnButton(index)}
                {comma}
            </div>
        );
    }

    renderPivotColumnDropdown(item, index) {
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
                <div style={pivotColumnSectionStyle}>
                    <ComboBox
                        className="ms-font-m"
                        options={options}
                        placeholder="column name"
                        selectedKey={item}
                        onChange={(event, selection) => this.onUpdatePivotColumn(selection.key, index)}
                    />
                </div>
            );
        } else {
            return (
                <div style={pivotColumnSectionStyle}>
                    <TextField
                        className="ms-font-m"
                        spellCheck={false}
                        placeholder="column name"
                        value={item}
                        onChange={(event, value) => this.onUpdatePivotColumn(value, index)}
                    />
                </div>
            );
        }
    }

    renderAddPivotColumnButton() {
        return (
            <DefaultButton
                className="rule-settings-button"
                style={addButtonStyle}
                title="Add new group by column"
                onClick={() => this.onAddPivotColumn()}
            >
                <i style={IconButtonStyles.greenStyle} className="ms-Icon ms-Icon--Add" />
                Add Column
            </DefaultButton>
        );
    }

    renderDeletePivotColumnButton(index) {
        return (
            <DefaultButton
                key="deletepivotcolumn"
                className="rule-settings-delete-button"
                style={deleteButtonStyle}
                title="Delete group by column"
                onClick={() => this.onDeletePivotColumn(index)}
            >
                <i style={IconButtonStyles.neutralStyle} className="ms-Icon ms-Icon--Delete" />
            </DefaultButton>
        );
    }

    onAddPivotColumn() {
        const pivots = [...this.props.rule.properties.pivots, ''];
        this.props.onUpdatePivots(pivots);
    }

    onDeletePivotColumn(index) {
        const pivots = [...this.props.rule.properties.pivots];
        pivots.splice(index, 1);
        this.props.onUpdatePivots(pivots);
    }

    onUpdatePivotColumn(value, index) {
        const pivots = [...this.props.rule.properties.pivots];
        pivots[index] = value;
        this.props.onUpdatePivots(pivots);
    }
}

// Props
RulePivotSettings.propTypes = {
    rule: PropTypes.object.isRequired,
    tableToSchemaMap: PropTypes.object.isRequired,

    onUpdatePivots: PropTypes.func.isRequired
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

const pivotColumnStyle = {
    display: 'flex',
    flexDirection: 'row'
};

const pivotColumnSectionStyle = {
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
