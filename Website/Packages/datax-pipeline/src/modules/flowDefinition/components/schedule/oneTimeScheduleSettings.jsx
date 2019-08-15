// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import * as Helpers from '../../flowHelpers';
import * as Models from '../../flowModels';
import { TextField, Label, Dropdown } from 'office-ui-fabric-react';
import DateTimePicker from 'react-datetime-picker';

export default class OneTimeScheduleSettings extends React.Component {
    constructor(props) {
        super(props);
    }

    render() {
        return (
            <div style={rootStyle}>
                {this.renderAlias()}
                {this.renderTypeDisplayName()}
                {this.renderInterval()}
                {this.renderWindow()}
                {this.renderTimeRange()}
            </div>
        );
    }

    renderAlias() {
        return (
            <div style={sectionStyle}>
                <TextField
                    className="ms-font-m info-settings-textbox"
                    spellCheck={false}
                    label="Alias"
                    value={this.props.batch.id}
                    onChange={(event, value) => this.props.onUpdateBatchName(value)}
                    onGetErrorMessage={value => this.validateProperty(value)}
                />
            </div>
        );
    }

    renderTypeDisplayName() {
        return (
            <div style={batchTypeSection}>
                <TextField
                    className="ms-font-m info-settings-textbox"
                    spellCheck={false}
                    label="Batch Type"
                    disabled={true}
                    value={this.props.batchTypeDisplayName}
                />
            </div>
        );
    }

    renderTimeRange() {
        return (
            <div>
                {this.renderStartTime()}
                {this.renderEndTime()}
            </div>
        );
    }

    renderStartTime() {
        let maxDate = this.props.batch.properties.endTime ? new Date(this.props.batch.properties.endTime) : '';

        return (
            <div style={sectionStyle}>
                <Label className="ms-font-m info-settings-textbox">Start Time</Label>
                <DateTimePicker
                    className="ms-font-m info-settings-textbox"
                    disabled={this.props.batch.disabled}
                    value={this.props.batch.properties.startTime ? new Date(this.props.batch.properties.startTime) : ''}
                    maxDate={maxDate}
                    onChange={value => this.props.onUpdateBatchStartTime(value)}
                />
            </div>
        );
    }

    renderEndTime() {
        let minDate = this.props.batch.properties.startTime ? new Date(this.props.batch.properties.startTime) : '';

        return (
            <div style={sectionStyle}>
                <Label className="ms-font-m info-settings-textbox">End Time</Label>
                <DateTimePicker
                    className="ms-font-m info-settings-textbox"
                    disabled={this.props.batch.disabled}
                    value={this.props.batch.properties.endTime ? new Date(this.props.batch.properties.endTime) : ''}
                    minDate={minDate}
                    onChange={value => this.props.onUpdateBatchEndTime(value)}
                />
            </div>
        );
    }

    renderInterval() {
        return (
            <div style={sectionContainerStyle}>
                {this.renderBatchSettingValue('Interval', this.props.batch.properties.interval, this.props.onUpdateBatchIntervalValue)}
                {this.renderBatchSettingUnitType(this.props.batch.properties.intervalType, this.props.onUpdateBatchIntervalType)}
            </div>
        );
    }

    renderWindow() {
        return (
            <div style={sectionContainerStyle}>
                {this.renderBatchSettingValue('Window', this.props.batch.properties.window, this.props.onUpdateBatchWindowValue)}
                {this.renderBatchSettingUnitType(this.props.batch.properties.windowType, this.props.onUpdateBatchWindowType)}
            </div>
        );
    }

    renderBatchSettingValue(type, value, onUpdateValue) {
        return (
            <div style={sectionValueStyle}>
                <TextField
                    className="ms-font-m"
                    disabled={this.props.batch.disabled}
                    spellCheck={false}
                    label={type}
                    value={value}
                    onChange={(event, value) => onUpdateValue(value)}
                />
            </div>
        );
    }

    renderBatchSettingUnitType(value, onUpdateType) {
        const options = Models.batchIntervalTypes.map(type => {
            return {
                key: type.key,
                text: type.name,
                disabled: type.disabled
            };
        });

        return (
            <div style={sectionDropdownStyle}>
                <Label className="ms-font-m">Unit</Label>
                <Dropdown
                    disabled={this.props.batch.disabled}
                    className="ms-font-m"
                    options={options}
                    selectedKey={value}
                    onChange={(event, selection) => onUpdateType(selection.key)}
                />
            </div>
        );
    }

    validateProperty(value) {
        if (value === '') return '';
        return !Helpers.isNumberAndStringOnly(value) ? 'Letters, numbers, and underscores only' : '';
    }
}

// Props
OneTimeScheduleSettings.propTypes = {
    batch: PropTypes.object.isRequired,
    // functions
    onUpdateBatchName: PropTypes.func.isRequired,
    onUpdateBatchStartTime: PropTypes.func.isRequired,
    onUpdateBatchEndTime: PropTypes.func.isRequired,
    onUpdateBatchIntervalValue: PropTypes.func.isRequired,
    onUpdateBatchIntervalType: PropTypes.func.isRequired,
    onUpdateBatchWindowValue: PropTypes.func.isRequired,
    onUpdateBatchWindowType: PropTypes.func.isRequired
};

// Styles
const rootStyle = {
    paddingLeft: 30,
    paddingRight: 30,
    paddingBottom: 30
};

const sectionStyle = {
    paddingBottom: 15
};

const batchTypeSection = {
    paddingBottom: 40
};

const sectionContainerStyle = {
    display: 'flex',
    flexDirection: 'row',
    width: 400
};

const sectionValueStyle = {
    flex: 1,
    marginRight: 10
};

const sectionDropdownStyle = {
    flex: 1,
    paddingBottom: 15
};
