// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import { Colors } from 'datax-common';
import { Link } from 'office-ui-fabric-react';

export default class Card extends React.Component {
    render() {
        return (
            <div style={rootStyle}>
                {this.renderImage()}
                <div style={contentStyle}>
                    {this.renderTitle()}
                    {this.renderDescription()}
                    {this.renderLink()}
                </div>
            </div>
        );
    }

    renderImage() {
        return (
            <a style={imageClickStyle} target="_blank" href={this.props.url} rel="noopener noreferrer">
                <div style={imageContainerStyle}>
                    <img style={imageStyle} src={this.props.src} />
                </div>
            </a>
        );
    }

    renderTitle() {
        return (
            <div className="ms-fontSize-l" style={titleContainerStyle} title={this.props.title}>
                {this.props.title}
            </div>
        );
    }

    renderDescription() {
        return (
            <div className="ms-fontSize-s" style={descriptionContainerStyle}>
                {this.props.description}
            </div>
        );
    }

    renderLink() {
        return (
            <div className="ms-fontSize-m" style={footerContainerStyle}>
                <Link style={linkStyle} target="_blank" href={this.props.url} rel="noopener noreferrer">
                    {this.props.linkText}
                </Link>
            </div>
        );
    }
}

const rootStyle = {
    marginRight: 30,
    marginTop: 16,
    marginBottom: 14,
    minWidth: 300,
    width: 400,
    maxWidth: 400,
    color: Colors.neutralPrimary,
    backgroundColor: Colors.white,
    border: `1px solid ${Colors.neutralQuaternaryAlt}`,
    boxShadow: `2px 2px 5px 0 rgba(218,218,218,.75)`
};

const imageClickStyle = {
    textDecoration: 'none'
};

const imageContainerStyle = {
    paddingLeft: 10,
    paddingRight: 10,
    paddingTop: 20,
    paddingBottom: 10,
    borderBottom: `1px solid ${Colors.neutralLight}`
};

const imageStyle = {
    width: '100%',
    height: 130
};

const contentStyle = {
    position: 'relative'
};

const titleContainerStyle = {
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
    paddingLeft: 16,
    paddingRight: 16,
    paddingTop: 14,
    paddingBottom: 10
};

const descriptionContainerStyle = {
    paddingLeft: 16,
    paddingRight: 16,
    paddingTop: 0,
    paddingBottom: 16,
    height: 65
};

const footerContainerStyle = {
    paddingLeft: 16,
    paddingRight: 16,
    paddingTop: 0,
    paddingBottom: 12,
    width: 220,
    position: 'absolute',
    bottom: 0
};

const linkStyle = {
    color: Colors.customBlue
};
