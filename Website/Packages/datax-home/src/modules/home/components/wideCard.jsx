// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import { Colors } from 'datax-common';

export default class WideCard extends React.Component {
    render() {
        return (
            <div style={rootStyle}>
                <div style={clickableCardStyle} onClick={this.props.onClick}>
                    {this.renderImage()}
                    <div style={contentStyle}>
                        {this.renderTitle()}
                        {this.renderDescription()}
                    </div>
                </div>
            </div>
        );
    }

    renderImage() {
        return (
            <div style={imageContainerStyle}>
                <img style={imageStyle} src={this.props.src} />
            </div>
        );
    }

    renderTitle() {
        return (
            <div className="ms-fontSize-l" style={titleContainerStyle}>
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
}

const rootStyle = {
    marginRight: 30,
    marginTop: 16,
    marginBottom: 14,
    color: Colors.neutralPrimary,
    backgroundColor: Colors.white,
    border: `1px solid ${Colors.neutralQuaternaryAlt}`,
    boxShadow: `2px 2px 5px 0 rgba(218,218,218,.75)`
};

const clickableCardStyle = {
    display: 'flex',
    flexDirection: 'row',
    minWidth: 300,
    width: 400,
    maxWidth: 400,
    height: 120,
    cursor: 'pointer'
};

const imageContainerStyle = {
    borderRight: `1px solid ${Colors.neutralLight}`,
    width: 60,
    padding: 30
};

const imageStyle = {
    width: '100%',
    height: '100%'
};

const contentStyle = {
    display: 'flex',
    flexDirection: 'column',
    flex: 1
};

const titleContainerStyle = {
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
    paddingLeft: 16,
    paddingRight: 16,
    paddingTop: 20,
    paddingBottom: 10
};

const descriptionContainerStyle = {
    paddingLeft: 16,
    paddingRight: 16,
    paddingTop: 0,
    paddingBottom: 16
};
