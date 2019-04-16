// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import Radium from 'radium';
import PropTypes from 'prop-types';
import { Colors } from '../styles';

const TopNavButton = Radium(
    class TopNavButton extends React.Component {
        render() {
            const title = this.props.title || '';
            const target = this.props.openNewTab || this.props.openNewTab === undefined ? '_blank' : '_self';

            return (
                <div style={rootStyle}>
                    <a href={this.props.url} style={contentStyle} target={target} title={title}>
                        <i className={`ms-Icon ms-Icon--${this.props.icon}`} />
                    </a>
                </div>
            );
        }
    }
);

// Props
TopNavButton.propTypes = {
    icon: PropTypes.string.isRequired, // Uses Fabric icons - https://developer.microsoft.com/en-us/fabric#/styles/icons#icons
    url: PropTypes.string.isRequired,
    title: PropTypes.string,
    openNewTab: PropTypes.bool
};

export default TopNavButton;

// Styles
const rootStyle = {
    color: Colors.white,
    height: 41,
    width: 40,
    minWidth: 40,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    borderRight: `1px solid ${Colors.white}`,
    ':hover': {
        backgroundColor: Colors.customBlueDark
    }
};

const contentStyle = {
    display: 'block',
    textAlign: 'center',
    lineHeight: 20,
    fontSize: 18
};
