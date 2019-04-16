// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import Radium from 'radium';
import PropTypes from 'prop-types';
import { Colors } from '../styles';

const StatementBox = Radium(
    class StatementBox extends React.Component {
        render() {
            const overrideStyle = this.props.overrideRootStyle ? this.props.overrideRootStyle : rootStyle;

            return (
                <div style={overrideStyle}>
                    <div style={[contentStyle, this.props.style]}>
                        <div style={iconContainerStyle}>
                            <i className={`ms-Icon ms-Icon--${this.props.icon}`} style={iconStyle} />
                        </div>
                        <div style={statementContainerStyle}>
                            {this.props.title && (
                                <div className="ms-font-m-plus" style={statementHeaderStyle}>
                                    {this.props.title}
                                </div>
                            )}
                            <div className="ms-font-m" style={inlineStatementStyle}>
                                {this.props.statement}
                            </div>
                        </div>
                    </div>
                </div>
            );
        }
    }
);

// Props
StatementBox.propTypes = {
    icon: PropTypes.string.isRequired,
    title: PropTypes.string,
    statement: PropTypes.string.isRequired,
    overrideRootStyle: PropTypes.object,
    style: PropTypes.object
};

export default StatementBox;

// Styles
const rootStyle = {
    paddingLeft: 30,
    paddingRight: 30,
    paddingTop: 30,
    paddingBottom: 30,
    borderBottom: `1px solid ${Colors.neutralTertiaryAlt}`
};

const contentStyle = {
    border: `1px solid ${Colors.themeLight}`,
    color: Colors.neutralSecondary,
    backgroundColor: Colors.themeLighterAlt,
    display: 'flex',
    flexDirection: 'row',
    alignItems: 'center'
};

const iconContainerStyle = {
    paddingLeft: 10,
    paddingRight: 10,
    paddingTop: 15,
    paddingBottom: 15,
    color: Colors.customBlue
};

const iconStyle = {
    fontSize: 36,
    paddingTop: 3,
    paddingLeft: 8
};

const statementContainerStyle = {
    paddingLeft: 10,
    paddingRight: 10,
    paddingTop: 15,
    paddingBottom: 15,
    flex: 1
};

const statementHeaderStyle = {
    fontWeight: 600,
    paddingRight: 10
};

const inlineStatementStyle = {
    display: 'inline'
};
