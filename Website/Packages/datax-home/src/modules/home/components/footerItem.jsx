// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import { Colors } from 'datax-common';
import { Link } from 'office-ui-fabric-react';

export default class FooterItem extends React.Component {
    render() {
        return (
            <div style={rootStyle}>
                {this.renderImage()}
                <div style={contentStyle}>
                    {this.renderDescription()}
                    {this.renderLink()}
                </div>
            </div>
        );
    }

    renderImage() {
        const containerStyle = this.props.largeImage ? largeImageContainerStyle : smallImageContainerStyle;
        return (
            <div style={containerStyle}>
                <img style={imageStyle} src={this.props.src} />
            </div>
        );
    }

    renderDescription() {
        return (
            <div className="ms-fontSize-m" style={descriptionContainerStyle}>
                {this.props.description}
            </div>
        );
    }

    renderLink() {
        return (
            <div className="ms-fontSize-m">
                <Link style={linkStyle} target="_blank" href={this.props.url} rel="noopener noreferrer">
                    {this.props.urlText}
                </Link>
            </div>
        );
    }
}

const rootStyle = {
    marginRight: 30,
    marginBottom: 20,
    color: Colors.neutralPrimary,
    backgroundColor: Colors.white,
    display: 'flex',
    flexDirection: 'row'
};

const smallImageContainerStyle = {
    width: 32,
    padding: 4
};

const largeImageContainerStyle = {
    width: 40
};

const imageStyle = {
    width: '100%',
    height: '100%'
};

const contentStyle = {
    display: 'flex',
    flexDirection: 'column',
    flex: 1,
    paddingLeft: 16,
    paddingRight: 16
};

const descriptionContainerStyle = {
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap'
};

const linkStyle = {
    color: Colors.customBlue
};
