// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react';
import { Colors } from 'datax-common';

class MetricWidgetPercentageBox extends React.Component {
    constructor() {
        super();
    }

    render() {
        let percentage = NaN;
        if (!isNaN(this.props.value) && !isNaN(this.props.baseValue) && this.props.baseValue != 0) {
            percentage = this.props.value / this.props.baseValue;
        }

        return (
            <div
                style={{
                    width: 300,
                    height: 120,
                    padding: 10,
                    borderColor: Colors.customGray,
                    borderWidth: 1,
                    borderStyle: 'solid',
                    backgroundColor: Colors.white,
                    textAlign: 'center',
                    marginRight: 20
                }}
            >
                <div
                    style={{
                        height: 60,
                        fontSize: 48,
                        fontWeight: 'bolder',
                        textAlign: 'center',
                        color: Colors.black,
                        paddingTop: 10
                    }}
                >
                    {isNaN(percentage) ? <Spinner size={SpinnerSize.large} /> : this.props.percentageFormat(percentage)}
                </div>
                <div
                    style={{
                        fontSize: 14,
                        textAlign: 'center',
                        color: Colors.black
                    }}
                >
                    <div>
                        {this.props.valueFormat(this.props.value)}/{this.props.valueFormat(this.props.baseValue)}
                    </div>
                </div>
                <div
                    style={{
                        marginTop: 10,
                        bottom: 0,
                        fontSize: 14,
                        fontWeight: 'bold',
                        textAlign: 'center',
                        color: Colors.black
                    }}
                >
                    <div>{this.props.title}</div>
                </div>
            </div>
        );
    }
}

export default MetricWidgetPercentageBox;
