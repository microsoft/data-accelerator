// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import * as Actions from './actions';

const INITIAL_METRIC_STATE = {
    items: undefined
};

export default (state = INITIAL_METRIC_STATE, action) => {
    switch (action.type) {
        case Actions.METRICS_RECEIVE_PRODUCTS:
            return Object.assign({}, state, { items: action.payload });

        default:
            return state;
    }
};
