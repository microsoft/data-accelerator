// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import Radium from 'radium';
import PropTypes from 'prop-types';
import Panel from './panel';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react';

const LoadingPanel = Radium(
    class LoadingPanel extends React.Component {
        render() {
            if (this.props.pastDelay || this.props.showImmediately) {
                let spinner;
                if (this.props.message) {
                    spinner = <Spinner size={SpinnerSize.large} label={this.props.message} />;
                } else {
                    spinner = <Spinner size={SpinnerSize.large} />;
                }

                return (
                    <Panel>
                        <div style={[spinnerContainerStyle, this.props.style]}>{spinner}</div>
                    </Panel>
                );
            } else {
                return null;
            }
        }
    }
);

// Props
LoadingPanel.propTypes = {
    pastDelay: PropTypes.bool,
    showImmediately: PropTypes.bool,
    message: PropTypes.string,
    style: PropTypes.object
};

export default LoadingPanel;

// Styles
const spinnerContainerStyle = {
    flex: 1,
    display: 'flex',
    height: '100%',
    alignItems: 'center',
    justifyContent: 'center'
};
