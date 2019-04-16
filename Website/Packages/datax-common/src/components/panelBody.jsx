// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import Radium from 'radium';
import PropTypes from 'prop-types';

const PanelBody = Radium(
    class PanelBody extends React.Component {
        render() {
            return <div style={[rootStyle, this.props.style]}>{this.props.children}</div>;
        }
    }
);

// Props
PanelBody.propTypes = {
    style: PropTypes.object
};

export default PanelBody;

// Styles
const rootStyle = {
    paddingTop: 10,
    paddingRight: 10,
    paddingBottom: 0,
    paddingLeft: 10,
    overflowX: 'hidden',
    overflowY: 'auto',
    height: '100%',
    width: '100%',
    display: 'flex',
    flexDirection: 'column'
};
