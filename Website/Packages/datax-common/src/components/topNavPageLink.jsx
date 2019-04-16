// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import Radium from 'radium';
import PropTypes from 'prop-types';
import { Colors } from '../styles';

const TopNavPageLink = Radium(
    class TopNavPageLink extends React.Component {
        render() {
            const style = [styles.base, this.props.selected && styles.selected];
            return (
                <div className="top-nav-page-link" style={style}>
                    {this.props.children}
                </div>
            );
        }
    }
);

// Props
TopNavPageLink.propTypes = {
    selected: PropTypes.bool
};

export default TopNavPageLink;

// Styles
const styles = {
    base: {
        color: Colors.neutralLight,
        fontSize: 17,
        fontWeight: 300,
        paddingTop: 12,
        paddingLeft: 8,
        paddingRight: 8,
        marginLeft: 12,
        marginRight: 12,
        marginBottom: 0,
        minWidth: 40,
        whiteSpace: 'nowrap',
        overflow: 'hidden',
        textOverflow: 'ellipsis',
        boxSizing: 'border-box'
    },
    selected: {
        color: Colors.white,
        fontWeight: 600
    }
};
