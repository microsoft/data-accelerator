// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import Radium from 'radium';
import PropTypes from 'prop-types';
import { Colors } from '../styles';

const PageHeader = Radium(
    class PageHeader extends React.Component {
        render() {
            return (
                <h1 style={[rootStyle, this.props.style]}>
                    {this.props.children}
                    {this.getHideButton()}
                </h1>
            );
        }

        getHideButton() {
            if (this.props.showHideButton) {
                return (
                    <div style={hideButtonStyle} onClick={this.props.onHideClick} title={this.props.hideTooltip}>
                        <i className="ms-Icon ms-Icon--ChevronLeft" />
                    </div>
                );
            } else {
                return null;
            }
        }
    }
);

// Props
PageHeader.propTypes = {
    style: PropTypes.object,
    showHideButton: PropTypes.bool,
    onHideClick: PropTypes.func,
    hideTooltip: PropTypes.string
};

export default PageHeader;

// Styles
const rootStyle = {
    fontWeight: 400,
    color: Colors.white,
    backgroundColor: Colors.customNeutralDarkGray,
    whiteSpace: 'nowrap',
    margin: 0,
    paddingTop: 9,
    paddingRight: 10,
    paddingBottom: 9,
    paddingLeft: 10
};

const hideButtonStyle = {
    color: Colors.white,
    cursor: 'pointer',
    display: 'inline',
    float: 'right',
    width: 20,
    ':hover': {
        backgroundColor: Colors.customYellow,
        color: Colors.neutralPrimary,
        fontWeight: 600
    }
};
