// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import * as Actions from './actions';

const INITIAL_USER_INFO_STATE = null;

export default (state = INITIAL_USER_INFO_STATE, action) => {
    switch (action.type) {
        case Actions.USER_RECEIVE_USER_INFO:
            return Object.freeze(action.payload.info);

        default:
            return state;
    }
};
