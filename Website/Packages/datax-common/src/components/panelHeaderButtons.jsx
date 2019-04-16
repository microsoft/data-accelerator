// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import Radium from 'radium';
import PropTypes from 'prop-types';
import { Colors } from '../styles';

const PanelHeaderButtons = Radium(
    class PanelHeaderButtons extends React.Component {
        render() {
            return <div style={[rootStyle, this.props.style]}>{this.props.children}</div>;
        }
    }
);

// Props
PanelHeaderButtons.propTypes = {
    style: PropTypes.object
};

export default PanelHeaderButtons;

// Styles
const rootStyle = {
    backgroundColor: Colors.neutralLight,
    borderBottom: `1px solid ${Colors.neutralTertiaryAlt}`
};
