// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import { Colors } from '../styles';

export default class ScrollableContentPane extends React.Component {
    render() {
        const paddingTop = this.props.showExtraTopPadding ? 5 : 0;
        const backgroundColor = this.props.backgroundColor ? this.props.backgroundColor : Colors.neutralLight;

        const rootStyle = {
            height: '100%',
            backgroundColor: backgroundColor,
            position: 'relative',
            display: 'flex',
            flexDirection: 'column',
            paddingTop: paddingTop,
            overflow: 'hidden'
        };

        const fadeStyle = {
            height: 25,
            position: 'absolute',
            left: 0,
            right: 0,
            background: `linear-gradient(to bottom, ${backgroundColor} 5px, rgba(0, 0, 0, 0) 20px)`,
            zIndex: 1
        };

        return (
            <div style={rootStyle}>
                <div style={fadeStyle} />
                <div style={contentStyle}>{this.props.children}</div>
            </div>
        );
    }
}

// Props
ScrollableContentPane.propTypes = {
    showExtraTopPadding: PropTypes.bool,
    backgroundColor: PropTypes.string
};

// Styles
const contentStyle = {
    flex: 1,
    width: '100%',
    height: '100%',
    overflowX: 'hidden',
    overflowY: 'auto',
    paddingTop: 20,
    paddingBottom: 20
};
