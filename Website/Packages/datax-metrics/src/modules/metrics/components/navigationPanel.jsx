// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import Radium from 'radium';
import { Colors, Panel, PanelHeader } from 'datax-common';

class NavigationPanel extends React.Component {
    render() {
        return (
            <Panel style={panelStyle}>
                <PanelHeader>Flows</PanelHeader>
                <div style={panelItemContainerStyle}>{this.props.children}</div>
            </Panel>
        );
    }
}

// Styles
const panelStyle = {
    width: 320,
    minWidth: 200,
    backgroundColor: Colors.neutralTertiaryAlt
};

const panelItemContainerStyle = {
    display: 'flex',
    flexDirection: 'column',
    overflowY: 'auto',
    paddingTop: 15,
    paddingBottom: 5
};

export default Radium(NavigationPanel);
