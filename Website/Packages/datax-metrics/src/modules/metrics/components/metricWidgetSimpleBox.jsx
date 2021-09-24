// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react';
import { Colors } from 'datax-common';

class MetricWidgetSimpleBox extends React.Component {
    constructor() {
        super();
    }

    render() {
        return (
            <div
                style={{
                    width: 300,
                    height: 120,
                    padding: 10,
                    borderColor: Colors.customNeutralDarkGray,
                    borderWidth: 1,
                    borderStyle: 'solid',
                    backgroundColor: Colors.white,
                    textAlign: 'center',
                    marginRight: 20
                }}
            >
                <div
                    style={{
                        height: 80,
                        fontSize: 48,
                        fontWeight: 'bolder',
                        textAlign: 'center',
                        color: Colors.black,
                        paddingTop: 10
                    }}
                >
                    {this.props.value === undefined ? <Spinner size={SpinnerSize.large} /> : this.props.format(this.props.value)}
                </div>
                <div
                    style={{
                        fontSize: 8,
                        textAlign: 'center',
                        color: Colors.black
                    }}
                >
                    <div>{this.props.subtitle}</div>
                </div>
                <div
                    style={{
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

export default MetricWidgetSimpleBox;
