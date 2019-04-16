// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import Radium from 'radium';
import { Link } from 'react-router-dom';
import { Colors } from 'datax-common';

class NavigationItem extends React.Component {
    render() {
        return (
            <div className="ms-fontSize-m" style={rootStyle}>
                <div style={labelStyle}>
                    <Link to={this.props.link}>{this.props.displayName}</Link>
                </div>
            </div>
        );
    }
}

// Styles
const rootStyle = {
    flex: 1,
    paddingLeft: 5,
    paddingRight: 5,
    paddingBottom: 8,
    paddingTop: 5,
    color: Colors.neutralLight,
    ':hover': {
        backgroundColor: Colors.neutralTertiary,
        cursor: 'pointer'
    }
};

const labelStyle = {
    marginLeft: 15,
    color: Colors.neutralPrimary,
    textDecoration: 'none'
};

export default Radium(NavigationItem);
