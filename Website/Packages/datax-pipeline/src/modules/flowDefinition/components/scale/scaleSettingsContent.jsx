// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import { Slider } from 'office-ui-fabric-react';
import { Colors, ScrollableContentPane, StatementBox } from 'datax-common';

export default class ScaleSettingsContent extends React.Component {
    constructor(props) {
        super(props);
    }

    render() {
        return (
            <div style={rootStyle}>
                <StatementBox icon="SpeedHigh" statement="Define how the Flow should scale for your processing." />
                <ScrollableContentPane backgroundColor={Colors.neutralLighterAlt}>{this.renderContent()}</ScrollableContentPane>
            </div>
        );
    }

    renderContent() {
        return (
            <div style={contentStyle}>
                <div style={sectionStyle}>
                    <Slider
                        className="ms-font-m info-settings-slider"
                        label="Number of Executors"
                        disabled={!this.props.scaleNumExecutorsSliderEnabled}
                        min={1}
                        max={500}
                        step={1}
                        value={Number(this.props.scale.jobNumExecutors)}
                        showValue={true}
                        onChange={value => this.props.onUpdateNumExecutors(value.toString())}
                    />
                </div>

                <div style={sectionStyle}>
                    <Slider
                        className="ms-font-m info-settings-slider"
                        label="Executor Memory (MB)"
                        disabled={!this.props.scaleExecutorMemorySliderEnabled}
                        min={1000}
                        max={16000}
                        step={1000}
                        value={Number(this.props.scale.jobExecutorMemory)}
                        showValue={true}
                        onChange={value => this.props.onUpdateExecutorMemory(value.toString())}
                    />
                </div>
            </div>
        );
    }
}

// Props
ScaleSettingsContent.propTypes = {
    scale: PropTypes.object.isRequired,

    onUpdateNumExecutors: PropTypes.func.isRequired,
    onUpdateExecutorMemory: PropTypes.func.isRequired,

    scaleNumExecutorsSliderEnabled: PropTypes.bool.isRequired,
    scaleExecutorMemorySliderEnabled: PropTypes.bool.isRequired
};

// Styles
const rootStyle = {
    display: 'flex',
    flexDirection: 'column',
    overflowY: 'hidden'
};

const contentStyle = {
    paddingLeft: 30,
    paddingRight: 30,
    paddingBottom: 30
};

const sectionStyle = {
    paddingBottom: 15
};
