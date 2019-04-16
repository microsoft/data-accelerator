// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import Radium from 'radium';
import * as PropTypes from 'prop-types';
import { Colors } from '../styles';
import TopNavIdentityPhoto from './topNavIdentityPhoto';
import TopNavIdentityMenu from './topNavIdentityMenu';

const TopNavIdentity = Radium(
    class TopNavIdentity extends React.Component {
        constructor(props) {
            super(props);

            this.state = {
                menuOpen: false
            };
        }

        render() {
            const menu = this.state.menuOpen ? <TopNavIdentityMenu logoutUrl={this.props.logoutUrl} /> : null;
            const wrapperStyle = [wrapperStyles.base, this.state.menuOpen && wrapperStyles.menuOpen];

            return (
                <div style={rootStyle} role="button" onClick={() => this.handleClick()}>
                    <div style={wrapperStyle}>
                        <div style={containerStyle}>
                            <div style={emailStyle}>{this.props.userEmail}</div>
                            <div style={nameStyle}>{this.props.userName}</div>
                        </div>
                        <TopNavIdentityPhoto url={this.props.photoUrl} />
                    </div>
                    {menu}
                </div>
            );
        }

        handleClick() {
            if (this.props.logoutUrl) {
                this.setState({
                    menuOpen: !this.state.menuOpen
                });
            }
        }
    }
);

// Props
TopNavIdentity.propTypes = {
    userEmail: PropTypes.string,
    userName: PropTypes.string,
    photoUrl: PropTypes.string,
    logoutUrl: PropTypes.string
};

export default TopNavIdentity;

// Styles
const rootStyle = {
    display: 'flex',
    position: 'relative',
    color: Colors.white,
    cursor: 'pointer'
};

const wrapperStyles = {
    base: {
        paddingLeft: 20,
        display: 'flex',
        alignItems: 'center',
        ':hover': {
            backgroundColor: Colors.customBlueDark
        }
    },
    menuOpen: {
        color: Colors.black,
        backgroundColor: Colors.white,
        ':hover': {
            backgroundColor: Colors.neutralLight
        }
    }
};

const containerStyle = {
    textAlign: 'right'
};

const emailStyle = {
    maxWidth: '160px',
    whiteSpace: 'nowrap',
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    fontSize: '14px'
};

const nameStyle = {
    maxWidth: '160px',
    whiteSpace: 'nowrap',
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    fontSize: '10px',
    textTransform: 'uppercase'
};
