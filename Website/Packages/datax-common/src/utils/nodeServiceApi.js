// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import Cookies from 'js-cookie';
import Q from 'q';

/**
 * Execute Node Service GET api
 *
 * @param {string} api - api route for Node Service Web Api
 */
export function nodeServiceGetApi(api) {
    const options = {
        method: 'GET'
    };

    return fetch(PrefixNodeApi(api), options).then(checkFetchResponse);
}

/**
 * Execute Node Service POST api
 *
 * @param {string} api - api route for Node Service Web Api
 * @param {object} body - parameter object of POST api
 */
export function nodeServicePostApi(api, body) {
    const options = {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'CSRF-Token': Cookies.get('csrfToken') || null,
            Accept: 'application/json'
        },
        body: body ? JSON.stringify(body) : undefined
    };

    return fetch(PrefixNodeApi(api), options).then(checkPostResponse);
}

const PrefixNodeApi = api => `/api/${api}`;

function checkFetchResponse(response) {
    if (response.ok) {
        return response.json().then(result => (result.error ? Q.reject(result.error) : result));
    } else {
        return Q.reject(`${response.status} - ${response.statusText}`);
    }
}

function checkPostResponse(response) {
    if (!response.ok) {
        return Q.reject(`${response.status} - ${response.statusText}`);
    }

    return response.json().then(result => {
        if (result.error) {
            return Q.reject(result.error);
        }

        if (result.resultCode && result.resultCode !== 0) {
            return Q.reject(`Transaction failed - result code is ${result.resultCode}`);
        }

        return result;
    });
}
