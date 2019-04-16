// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import Radium from 'radium';
import PropTypes from 'prop-types';
import { Colors } from '../styles';

const TopNavIdentityMenu = Radium(
    class TopNavIdentityMenu extends React.Component {
        render() {
            return (
                <div style={style}>
                    <ul style={listStyle}>
                        <li style={listItemStyle}>
                            <a style={linkStyle} href={this.props.logoutUrl}>
                                Logout
                            </a>
                        </li>
                    </ul>
                </div>
            );
        }
    }
);

// Props
TopNavIdentityMenu.propTypes = {
    logoutUrl: PropTypes.string.isRequired
};

export default TopNavIdentityMenu;

// Styles
const style = {
    position: 'absolute',
    top: 40,
    right: 0,
    width: 220,
    backgroundColor: Colors.white,
    color: Colors.black,
    boxShadow: '0 10px 20px -5px rgba(58,58,58,.25)',
    zIndex: 200
};

const listStyle = {
    listStyle: 'none',
    padding: 0,
    margin: 0
};

const listItemStyle = {
    whiteSpace: 'nowrap',
    ':hover': {
        backgroundColor: Colors.neutralLight
    }
};

const linkStyle = {
    display: 'block',
    padding: '8px 20px',
    textDecoration: 'none'
};
