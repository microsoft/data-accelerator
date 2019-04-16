// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import * as Actions from './layoutActions';

const INITIAL_LAYOUT_STATE = {
    isTestQueryOutputPanelVisible: false
};

export default (state = INITIAL_LAYOUT_STATE, action) => {
    switch (action.type) {
        case Actions.LAYOUT_SET_TESTQUERY_OUTPUT_PANEL_VISIBILITY:
            return Object.assign({}, state, { isTestQueryOutputPanelVisible: action.payload });

        default:
            return state;
    }
};
