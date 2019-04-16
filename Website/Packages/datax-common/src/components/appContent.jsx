// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import Radium from 'radium';
import PropTypes from 'prop-types';

const AppContent = Radium(
    class AppContent extends React.Component {
        render() {
            return <div style={[rootStyle, this.props.style]}>{this.props.children}</div>;
        }
    }
);

// Props
AppContent.propTypes = {
    style: PropTypes.object
};

export default AppContent;

// Styles
const rootStyle = {
    flex: 1,
    boxSizing: 'border-box',
    display: 'flex',
    flexDirection: 'column',
    overflow: 'hidden'
};
