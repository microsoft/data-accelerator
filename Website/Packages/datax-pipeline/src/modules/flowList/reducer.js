// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import * as Actions from './actions';

const INITIAL_FLOWSLIST_STATE = {
    items: undefined,
    errorMessage: undefined
};

export default (state = INITIAL_FLOWSLIST_STATE, action) => {
    switch (action.type) {
        case Actions.FLOWSLIST_RECEIVE_FLOWSLIST:
            let flowslist = action.payload;
            flowslist.sort((a, b) => {
                if (!a.displayName) return -1;
                if (!b.displayName) return 1;

                return a.displayName.localeCompare(b.displayName);
            });
            return Object.assign({}, state, { items: flowslist });

        case Actions.FLOWSLIST_UPDATE_ERRORMESSAGE:
            return Object.assign({}, state, { errorMessage: action.message });

        default:
            return state;
    }
};
