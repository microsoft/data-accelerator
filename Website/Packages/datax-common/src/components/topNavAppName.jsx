// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import { Colors } from '../styles';

export default class TopNavAppName extends React.Component {
    render() {
        return (
            <div style={rootStyle}>
                <div style={nameStyle}>{this.props.name}</div>
            </div>
        );
    }
}

// Props
TopNavAppName.propTypes = {
    name: PropTypes.string.isRequired
};

// Styles
const rootStyle = {
    position: 'relative',
    color: Colors.white,
    height: 40,
    paddingLeft: 10,
    paddingRight: 60,
    minWidth: 120,
    boxSizing: 'border-box',
    cursor: 'default'
};

const nameStyle = {
    fontSize: 20,
    lineHeight: '42px',
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap'
};
