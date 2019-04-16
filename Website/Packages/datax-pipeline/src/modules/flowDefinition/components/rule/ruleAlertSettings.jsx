// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import * as Models from '../../flowModels';
import { Label, Toggle, Dropdown, DropdownMenuItemType } from 'office-ui-fabric-react';
import { Colors } from 'datax-common';

export default class RuleAlertSettings extends React.Component {
    constructor(props) {
        super(props);
    }

    render() {
        const isAlert = this.props.rule.properties.isAlert;
        return (
            <div>
                <Label className="ms-font-m ms-fontWeight-semibold" style={titleStyle}>
                    Alerting
                </Label>

                <div style={sectionContainerStyle}>
                    <div style={toggleSectionStyle}>
                        <Toggle
                            label="Do you want to be alerted?"
                            onText="Yes"
                            offText="No"
                            checked={isAlert}
                            onChange={(event, value) => this.props.onUpdateIsAlert(value)}
                        />
                    </div>

                    {isAlert && <div style={sectionStyle}>{this.renderAlertSeverityDropdown()}</div>}
                    {isAlert && <div style={sectionStyle}>{this.renderAlertSinksDropdown()}</div>}
                </div>
            </div>
        );
    }

    renderAlertSinksDropdown() {
        let options = this.props.sinkers.map(sinker => {
            return {
                key: sinker.id,
                text: sinker.id,
                disabled: false
            };
        });

        options.push({
            key: 'sinkmessage',
            text: 'You can add more output sinks under Outputs tab',
            itemType: DropdownMenuItemType.Header
        });

        return (
            <div style={alertSinkDropdownStyle}>
                <Label className="ms-font-m">Output Sinks</Label>
                <Dropdown
                    className="ms-font-m"
                    multiSelect
                    options={options}
                    dropdownWidth={400}
                    selectedKeys={this.props.rule.properties.alertSinks}
                    onChange={(event, item) => this.onUpdateSinksMultiSelect(item)}
                />
            </div>
        );
    }

    renderAlertSeverityDropdown() {
        const options = Models.severityTypes.map(type => {
            return {
                key: type.key,
                text: type.name,
                disabled: type.disabled
            };
        });

        return (
            <div style={typeDropdownStyle}>
                <Label className="ms-font-m">Severity</Label>
                <Dropdown
                    className="ms-font-m"
                    options={options}
                    selectedKey={this.props.rule.properties.severity}
                    onChange={(event, selection) => this.props.onUpdateSeverity(selection.key)}
                />
            </div>
        );
    }

    onUpdateSinksMultiSelect(item) {
        const updatedSelectedItem = this.props.rule.properties.alertSinks ? [...this.props.rule.properties.alertSinks] : [];

        if (item.selected) {
            updatedSelectedItem.push(item.key);
        } else {
            const currentIndex = updatedSelectedItem.indexOf(item.key);
            if (currentIndex > -1) {
                updatedSelectedItem.splice(currentIndex, 1);
            }
        }

        this.props.onUpdateSinks(updatedSelectedItem);
    }
}

// Props
RuleAlertSettings.propTypes = {
    rule: PropTypes.object.isRequired,
    sinkers: PropTypes.array.isRequired,

    onUpdateIsAlert: PropTypes.func.isRequired,
    onUpdateSinks: PropTypes.func.isRequired,
    onUpdateSeverity: PropTypes.func.isRequired
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

const toggleSectionStyle = {
    paddingBottom: 29,
    paddingRight: 15,
    width: 200,
    minWidth: 200
};

const typeDropdownStyle = {
    width: 200,
    minWidth: 200
};

const alertSinkDropdownStyle = {
    width: 300,
    minWidth: 200
};
