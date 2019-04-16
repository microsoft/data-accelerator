// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import * as React from 'react';
import PropTypes from 'prop-types';
import { Colors } from '../styles';

export default class IFramePage extends React.Component {
    render() {
        return (
            <div style={rootStyle}>
                <div style={contentStyle}>
                    <iframe style={iframeHostStyle} src={this.props.url} height="100%" width="100%" />
                </div>
            </div>
        );
    }
}

// Props
IFramePage.propTypes = {
    url: PropTypes.string.isRequired
};

// Styles
const rootStyle = {
    display: 'flex',
    flexDirection: 'row',
    overflow: 'hidden',
    height: '100%',
    width: '100%',
    backgroundColor: Colors.neutralLighterAlt
};

const contentStyle = {
    flex: 1,
    display: 'flex',
    height: '100%',
    width: '100%'
};

const iframeHostStyle = {
    borderWidth: 0
};
