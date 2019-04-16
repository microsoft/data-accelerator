// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import Radium from 'radium';
import PropTypes from 'prop-types';
import { Colors } from '../styles';

const Panel = Radium(
    class Panel extends React.Component {
        render() {
            return <div style={[rootStyle, this.props.style]}>{this.props.children}</div>;
        }
    }
);

// Props
Panel.propTypes = {
    style: PropTypes.object
};

export default Panel;

// Styles
const rootStyle = {
    display: 'flex',
    flexDirection: 'column',
    overflow: 'hidden',
    height: '100%',
    width: '100%',
    backgroundColor: Colors.neutralLighterAlt
};
