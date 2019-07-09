// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import Radium from 'radium';
import PropTypes from 'prop-types';
import { Colors, Panel } from 'datax-common';

const SideToolbar = Radium(
    class SideToolbar extends React.Component {
        constructor(props) {
            super(props);
        }

        render() {
            return (
                <Panel style={panelStyle}>
                    <div style={itemContainerStyle}>{this.getExecuteQueryOutputPanel()}</div>
                </Panel>
            );
        }

        getExecuteQueryOutputPanel() {
            if (!this.props.isTestQueryOutputPanelVisible) {
                return (
                    <div
                        key="outputPanel"
                        style={itemStyle}
                        onClick={this.props.onShowTestQueryOutputPanel}
                        title="Show test query output window"
                    >
                        Output
                    </div>
                );
            } else {
                return null;
            }
        }
    }
);

// Props
SideToolbar.propTypes = {
    isTestQueryOutputPanelVisible: PropTypes.bool.isRequired,

    onShowTestQueryOutputPanel: PropTypes.func.isRequired
};

export default SideToolbar;

// Styles
const panelStyle = {
    position: 'fixed',
    right: 0,
    backgroundColor: Colors.neutralTertiaryAlt,
    width: 39
};

const itemContainerStyle = {
    transform: 'rotate(90deg) translateX(39px)',
    transformOrigin: '39px 0 0',
    height: 39,
    display: 'flex'
};

const itemStyle = {
    backgroundColor: Colors.customBlueDarker,
    color: Colors.white,
    fontSize: 15,
    paddingTop: 9,
    paddingBottom: 9,
    minWidth: 180,
    textAlign: 'center',
    borderRight: `3px solid ${Colors.neutralTertiaryAlt}`,
    cursor: 'pointer',
    whiteSpace: 'nowrap',
    ':hover': {
        backgroundColor: Colors.customYellow,
        color: Colors.neutralPrimary,
        fontWeight: 600
    }
};
