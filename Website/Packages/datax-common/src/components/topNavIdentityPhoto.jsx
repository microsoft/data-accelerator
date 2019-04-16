// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';

export default class TopNavIdentityPhoto extends React.Component {
    constructor(props) {
        super(props);

        this.state = {
            errorLoading: false
        };
    }

    render() {
        if (!this.props.url || this.state.errorLoading) {
            return <div style={noImageStyle} />;
        }

        return <img style={style} alt="user profile photo" src={this.props.url} onError={() => this.handleError()} />;
    }

    handleError() {
        this.setState({
            errorLoading: true
        });
    }
}

// Props
TopNavIdentityPhoto.propTypes = {
    url: PropTypes.string
};

// Styles
const style = {
    height: 36,
    width: 36,
    borderRadius: 36,
    marginLeft: 12,
    marginRight: 4
};

const noImageStyle = {
    width: 20
};
