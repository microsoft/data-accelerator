// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import * as Styles from '../styles';
import CenteredMessagePanel from './centeredMessagePanel';

export default class ErrorMessagePanel extends React.Component {
    render() {
        return (
            <CenteredMessagePanel
                message={this.props.message}
                hasIcon={true}
                icon="ms-Icon ms-Icon--IncidentTriangle"
                iconColor={Styles.Colors.red}
            />
        );
    }
}

// Props
ErrorMessagePanel.propTypes = {
    message: PropTypes.string.isRequired
};
