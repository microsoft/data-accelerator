// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import Radium from 'radium';
import PropTypes from 'prop-types';

const Shell = Radium(
    class Shell extends React.Component {
        render() {
            return <div style={[rootStyle, this.props.style]}>{this.props.children}</div>;
        }
    }
);

// Props
Shell.propTypes = {
    style: PropTypes.object
};

export default Shell;

// Styles
const rootStyle = {
    position: 'absolute',
    top: 40,
    bottom: 0,
    left: 0,
    right: 0,
    display: 'flex'
};
