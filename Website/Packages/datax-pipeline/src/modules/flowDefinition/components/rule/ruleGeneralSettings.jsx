// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import * as Models from '../../flowModels';
import { Label, TextField, ComboBox } from 'office-ui-fabric-react';
import { Colors } from 'datax-common';

export default class RuleGeneralSettings extends React.Component {
    constructor(props) {
        super(props);
    }

    render() {
        return (
            <div>
                <Label className="ms-font-m ms-fontWeight-semibold" style={titleStyle}>
                    Rule
                </Label>

                <div style={sectionContainerStyle}>
                    <div style={descriptionSectionStyle}>
                        <TextField
                            className="ms-font-m"
                            spellCheck={false}
                            label="Description"
                            value={this.props.rule.properties.ruleDescription}
                            onChange={(event, value) => this.props.onUpdateRuleDescription(value)}
                        />
                    </div>

                    <div style={tagSectionStyle}>
                        <TextField
                            className="ms-font-m"
                            spellCheck={false}
                            label="Tag"
                            value={this.props.rule.properties.tag}
                            onChange={(event, value) => this.props.onUpdateTag(value)}
                        />
                    </div>

                    <div style={sectionStyle}>{this.renderRuleSubTypeDropdown()}</div>

                    <div style={sectionStyle}>{this.renderSchemaTableNameDropdown()}</div>
                </div>
            </div>
        );
    }

    renderRuleSubTypeDropdown() {
        const options = Models.ruleSubTypes.map(type => {
            return {
                key: type.key,
                text: type.name,
                disabled: type.disabled
            };
        });

        return (
            <div style={typeDropdownStyle}>
                <ComboBox
                    className="ms-font-m"
                    label="Sub Type"
                    options={options}
                    selectedKey={this.props.rule.properties.ruleType}
                    onChange={(event, selection) => this.props.onUpdateRuleSubType(selection.key)}
                />
            </div>
        );
    }

    renderSchemaTableNameDropdown() {
        let options = Object.values(this.props.tableToSchemaMap).map(schema => {
            return {
                key: schema.name,
                text: schema.name
            };
        });

        if (!(Models.DefaultSchemaTableName in this.props.tableToSchemaMap)) {
            options.unshift({
                key: Models.DefaultSchemaTableName,
                text: Models.DefaultSchemaTableName
            });
        }

        return (
            <div style={typeDropdownStyle}>
                <ComboBox
                    className="ms-font-m"
                    label="Target Table"
                    options={options}
                    selectedKey={this.props.rule.properties.schemaTableName}
                    onChange={(event, selection) => this.props.onUpdateSchemaTableName(selection.key)}
                />
            </div>
        );
    }
}

// Props
RuleGeneralSettings.propTypes = {
    rule: PropTypes.object.isRequired,
    tableToSchemaMap: PropTypes.object.isRequired,

    onUpdateRuleSubType: PropTypes.func.isRequired,
    onUpdateRuleDescription: PropTypes.func.isRequired,
    onUpdateTag: PropTypes.func.isRequired,
    onUpdateSchemaTableName: PropTypes.func.isRequired
};

// Styles
const titleStyle = {
    color: Colors.customBlueDark,
    marginBottom: 5
};

const sectionContainerStyle = {
    display: 'flex',
    flexDirection: 'row',
    flexWrap: 'wrap',
    backgroundColor: Colors.neutralLighter,
    border: `1px solid ${Colors.neutralQuaternaryAlt}`,
    paddingLeft: 15,
    paddingRight: 0,
    paddingTop: 15,
    paddingBottom: 0,
    marginBottom: 20
};

const sectionStyle = {
    paddingBottom: 25,
    paddingRight: 15
};

const descriptionSectionStyle = {
    paddingBottom: 25,
    paddingRight: 15,
    width: 300,
    minWidth: 200
};

const tagSectionStyle = {
    paddingBottom: 25,
    paddingRight: 15,
    width: 200,
    minWidth: 200
};

const typeDropdownStyle = {
    width: 200,
    minWidth: 200
};
