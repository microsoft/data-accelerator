// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import { Colors } from '../styles';
import Icon from './icon';

export default class AppLogo extends React.Component {
    render() {
        return (
            <a style={rootStyle} href="/" aria-label="Data Accelerator Home Link">
                <Icon />
            </a>
        );
    }
}

// Styles
const rootStyle = {
    display: 'block',
    marginLeft: 14,
    marginRight: 6,
    marginTop: 6,
    boxSizing: 'border-box',
    color: Colors.white,
    ':hover': {
        backgroundColor: Colors.customBlueDark
    }
};
