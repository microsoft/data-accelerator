// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import * as Api from './api';
import Q from 'q';
import { getApiErrorMessage } from 'datax-common';

export const FLOWSLIST_RECEIVE_FLOWSLIST = 'FLOWSLIST_RECEIVE_FLOWSLIST';
export const FLOWSLIST_UPDATE_ERRORMESSAGE = 'FLOWSLIST_UPDATE_ERRORMESSAGE';

export function getFlowsList() {
    return dispatch =>
        Api.getFlowsList()
            .then(flowslist =>
                dispatch({
                    type: FLOWSLIST_RECEIVE_FLOWSLIST,
                    payload: flowslist
                })
            )
            .catch(error => {
                const message = getApiErrorMessage(error);
                updateErrorMessage(dispatch, message);
                return Q.reject({ error: true, message: message });
            });
}

function updateErrorMessage(dispatch, message) {
    return dispatch({
        type: FLOWSLIST_UPDATE_ERRORMESSAGE,
        message: message
    });
}
