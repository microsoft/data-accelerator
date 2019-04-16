// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import * as Actions from './kernelActions';

const INITIAL_KERNEL_STATE = {
    version: 0,
    kernelId: '',
    fetchingKernel: false
};

export default (state = INITIAL_KERNEL_STATE, action) => {
    switch (action.type) {
        case Actions.KERNEL_UPDATE_KERNEL_VERSION:
            return Object.assign({}, state, { kernelId: '', version: action.version });

        case Actions.KERNEL_UPDATE_KERNEL:
            return Object.assign({}, state, {
                kernelId: action.kernelId,
                version: action.version,
                fetchingKernel: false
            });

        case Actions.KERNEL_FETCHING_KERNEL:
            return Object.assign({}, state, { fetchingKernel: action.value });

        default:
            return state;
    }
};
