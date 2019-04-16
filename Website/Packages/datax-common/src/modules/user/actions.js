// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import * as Api from './api';

export const USER_RECEIVE_USER_INFO = 'USER_RECEIVE_USER_INFO';

export function getUserInfo() {
    return dispatch =>
        Api.getUser().then(info => {
            dispatch({
                type: USER_RECEIVE_USER_INFO,
                payload: {
                    info: info
                }
            });
        });
}
