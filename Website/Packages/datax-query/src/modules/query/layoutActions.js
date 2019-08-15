// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
export const LAYOUT_SET_TESTQUERY_OUTPUT_PANEL_VISIBILITY = 'LAYOUT_SET_TESTQUERY_OUTPUT_PANEL_VISIBILITY';

export const onShowTestQueryOutputPanel = isVisible => dispatch => {
    return dispatch({
        type: LAYOUT_SET_TESTQUERY_OUTPUT_PANEL_VISIBILITY,
        payload: isVisible
    });
};
