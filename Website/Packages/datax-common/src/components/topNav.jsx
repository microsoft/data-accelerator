// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import { Colors } from '../styles';
import AppLogo from './appLogo';
import TopNavAppName from './topNavAppName';
import TopNavSpacer from './topNavSpacer';
import TopNavIdentity from './topNavIdentity';

export default class TopNav extends React.Component {
    render() {
        return (
            <div style={style}>
                {this.props.showAppLogo && <AppLogo />}
                <TopNavAppName name={this.props.appName} />
                {this.props.pages}
                <TopNavSpacer />
                {this.props.buttons}
                <TopNavIdentity
                    userEmail={this.props.identity.userEmail}
                    userName={this.props.identity.userName}
                    photoUrl={this.props.identity.photoUrl}
                    logoutUrl={this.props.logoutUrl}
                />
            </div>
        );
    }
}

// Props
TopNav.propTypes = {
    appName: PropTypes.string.isRequired,
    pages: PropTypes.array.isRequired,
    buttons: PropTypes.array.isRequired,
    identity: PropTypes.object.isRequired,
    logoutUrl: PropTypes.string,
    showAppLogo: PropTypes.bool
};

// Styles
const style = {
    backgroundColor: Colors.black,
    height: 40,
    display: 'flex'
};
