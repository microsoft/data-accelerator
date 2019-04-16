// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import { Panel, PanelHeader, LoadingPanel } from 'datax-common';

export default class LoadingPage extends React.Component {
    render() {
        return (
            <Panel>
                <PanelHeader>&nbsp;</PanelHeader>
                <LoadingPanel
                    pastDelay={this.props.pastDelay}
                    showImmediately={this.props.showImmediately}
                    message={this.props.message}
                    style={this.props.style}
                />
            </Panel>
        );
    }
}

// Props
LoadingPage.propTypes = {
    pastDelay: PropTypes.bool,
    showImmediately: PropTypes.bool,
    message: PropTypes.string,
    style: PropTypes.object
};
