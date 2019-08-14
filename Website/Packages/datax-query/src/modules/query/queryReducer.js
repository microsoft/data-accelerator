// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import * as Actions from './queryActions';
import * as Models from './queryModels';

const INITIAL_QUERY_STATE = {    
    query: Models.defaultQuery,    
    errorMessage: undefined,
    warningMessage: undefined,
    isDirty: false,
    resamplingInputDuration:'15'
};

export default (state = INITIAL_QUERY_STATE, action) => {
    switch (action.type) {
        
        case Actions.QUERY_INIT:
            const query = action.payload;
            return Object.assign({}, INITIAL_QUERY_STATE, query, {
                query: query
             });
            
        
       
        case Actions.QUERY_UPDATE_RESAMPLING_INPUT_DURATION:
            return Object.assign({}, state, {
                resamplingInputDuration: action.duration
            });
        
        case Actions.QUERY_UPDATE_QUERY:
            return Object.assign({}, state, {
                isDirty: true,
                query: action.payload
            });

        case Actions.QUERY_UPDATE_ERROR_MESSAGE:
            return Object.assign({}, state, { errorMessage: action.message });

        case Actions.QUERY_UPDATE_WARNING_MESSAGE:
            return Object.assign({}, state, { warningMessage: action.message });
        
        default:
            return state;
    }
};
