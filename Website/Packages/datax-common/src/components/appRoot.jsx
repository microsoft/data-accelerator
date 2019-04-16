// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import Radium from 'radium';
import PropTypes from 'prop-types';
import { Colors } from '../styles';

const AppRoot = Radium(
    class AppRoot extends React.Component {
        render() {
            return <div style={[rootStyle, this.props.style]}>{this.props.children}</div>;
        }
    }
);

// Props
AppRoot.propTypes = {
    style: PropTypes.object
};

export default AppRoot;

// Styles
const rootStyle = {
    height: '100%',
    color: Colors.neutralPrimary,
    backgroundColor: Colors.white,
    display: 'flex',
    flexDirection: 'column'
};
