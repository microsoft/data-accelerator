// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import { Colors } from 'datax-common';
import { PrimaryButton } from 'office-ui-fabric-react';

export default class WelcomeCard extends React.Component {
    render() {
        const descriptions = this.props.descriptions.map((description, index) => {
            return <div key={`welcome_description_${index}`}>{description}</div>;
        });
        return (
            <div style={rootStyle}>
                <div style={contentStyle}>
                    <div className="ms-fontSize-xl" style={titleContainerStyle}>
                        {this.props.title}
                    </div>
                    <div className="ms-fontSize-m" style={descriptionContainerStyle}>
                        {descriptions}
                    </div>
                    <div style={buttonContainerStyle}>
                        <PrimaryButton text={this.props.buttonText} onClick={this.props.onButtonClick} />
                    </div>
                </div>
                <div style={imageContainerStyle}>
                    <img style={imageStyle} src={this.props.src} />
                </div>
            </div>
        );
    }
}

const rootStyle = {
    display: 'flex',
    flexDirection: 'row',
    marginLeft: 30,
    marginRight: 30,
    color: Colors.neutralPrimary
};

const imageContainerStyle = {
    width: 600,
    paddingRight: 90
};

const imageStyle = {
    width: '100%',
    height: 325
};

const contentStyle = {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    margin: 'auto'
};

const titleContainerStyle = {
    fontSize: 24,
    paddingBottom: 16
};

const descriptionContainerStyle = {
    paddingBottom: 16
};

const buttonContainerStyle = {
    paddingTop: 20
};
