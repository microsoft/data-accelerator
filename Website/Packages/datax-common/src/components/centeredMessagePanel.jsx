// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import Radium from 'radium';
import PropTypes from 'prop-types';
import Panel from './panel';
import * as Styles from '../styles';

const CenteredMessagePanel = Radium(
    class CenteredMessagePanel extends React.Component {
        render() {
            let iconComponent = null;
            if (this.props.hasIcon) {
                const icon = this.props.icon || 'InfoSolid';
                const overrideIconStyle = this.props.iconColor ? { color: this.props.iconColor } : null;
                const iconStyle = [Styles.IconButtonStyles.blueStyle, iconSizeStyle, overrideIconStyle];
                iconComponent = <i style={iconStyle} className={`ms-Icon ms-Icon--${icon}`} />;
            }

            return (
                <Panel>
                    <div style={centeredTextStyle}>
                        {iconComponent}
                        {this.props.message}
                    </div>
                </Panel>
            );
        }
    }
);

// Props
CenteredMessagePanel.propTypes = {
    message: PropTypes.string.isRequired,
    hasIcon: PropTypes.bool,
    icon: PropTypes.string, // Uses Fabric icons - https://developer.microsoft.com/en-us/fabric#/styles/icons#icons
    iconColor: PropTypes.string
};

export default CenteredMessagePanel;

// Styles
const centeredTextStyle = {
    flex: 1,
    display: 'flex',
    height: '100%',
    alignItems: 'center',
    justifyContent: 'center'
};

const iconSizeStyle = {
    fontSize: 32
};
