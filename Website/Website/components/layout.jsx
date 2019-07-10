// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import { connect } from 'react-redux';
import { withRouter } from 'react-router';
import { Link } from 'react-router-dom';
import { AppRoot, TopNav, TopNavPageLink, TopNavButton, Shell, AppContent, Colors, UserActions, UserSelectors } from 'datax-common';

const photoUrl = '/api/user/photo';
const logoutUrl = '/logout';

class Layout extends React.Component {
    componentDidMount() {
        this.props.onGetUserInfo();
    }

    render() {
        let pages = (this.props.pages || []).map(page => this.getPageLink(page));
        let buttons = this.getButtons();

        return (
            <AppRoot>
                <TopNav
                    appName={this.props.displayName}
                    pages={pages}
                    buttons={buttons}
                    identity={{
                        userEmail: this.props.userInfo && this.props.userInfo.id,
                        userName: this.props.userInfo && this.props.userInfo.name,
                        photoUrl: photoUrl
                    }}
                    logoutUrl={logoutUrl}
                    showAppLogo={true}
                />
                <Shell>
                    <AppContent>
                        <div style={contentStyle}>{this.props.children}</div>
                    </AppContent>
                </Shell>
            </AppRoot>
        );
    }

    getPageLink(page) {
        const label = page.title;
        const route = page.url;

        if (page.external) {
            return (
                <TopNavPageLink key={label} selected={this.props.location.pathname == route}>
                    <a href={route} target="_blank" rel="noopener noreferrer">
                        {label}
                    </a>
                </TopNavPageLink>
            );
        } else {
            // Handle keeping root route highlighted on nav bar for all sub pages
            const isSelected = this.props.location.pathname.startsWith(route);
            return (
                <TopNavPageLink key={label} selected={isSelected}>
                    <Link to={{ pathname: route }}>{label}</Link>
                </TopNavPageLink>
            );
        }
    }

    getButtons() {
        return [
            <TopNavButton key="bug" icon="Bug" title="Report a bug" url={this.props.bugUrl} />,
            <TopNavButton key="email" icon="Emoji2" title="Provide feedback" url={this.props.emailUrl} />
        ];
    }
}

// Props
Layout.propTypes = {
    displayName: PropTypes.string.isRequired,
    pages: PropTypes.array.isRequired,
    emailUrl: PropTypes.string,
    bugUrl: PropTypes.string
};

// State Props
const mapStateToProps = state => ({
    userInfo: UserSelectors.getUserInfo(state)
});

// Dispatch Props
const mapDispatchToProps = dispatch => ({
    onGetUserInfo: () => dispatch(UserActions.getUserInfo())
});

// Styles
const contentStyle = {
    flex: 1,
    overflow: 'hidden',
    backgroundColor: Colors.neutralLighterAlt
};

export default withRouter(
    connect(
        mapStateToProps,
        mapDispatchToProps
    )(Layout)
);
