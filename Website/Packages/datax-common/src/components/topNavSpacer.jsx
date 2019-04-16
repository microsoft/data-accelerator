// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import { Colors } from '../styles';

export default class TopNavSpacer extends React.Component {
    render() {
        return <div style={style} />;
    }
}

// Styles
const style = {
    flex: 1,
    borderRight: `1px solid ${Colors.white}`
};
