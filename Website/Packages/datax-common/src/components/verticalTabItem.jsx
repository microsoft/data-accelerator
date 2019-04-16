// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import { Colors } from '../styles';

export default class VerticalTabItem extends React.Component {
    render() {
        return <div style={rootStyle}>{this.props.children}</div>;
    }
}

// Styles
const rootStyle = {
    display: 'flex',
    flexDirection: 'column',
    flex: 1,
    overflowY: 'hidden',
    backgroundColor: Colors.neutralLighterAlt
};
