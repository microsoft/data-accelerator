// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import Cookies from 'js-cookie';
import Q from 'q';

/**
 * Execute Service GET api
 *
 * @param {string} api - api route to application
 * @param {string} application - name of application
 * @param {string} service - name of service
 * @param {string} serviceApi - service api route
 * @param {object} params - paremeter object of GET api
 */
export function serviceGetApi(api, application, service, serviceApi, params) {
    return executeServiceApi(api, application, service, serviceApi, 'GET', null, params);
}

/**
 * Execute Service POST api
 *
 * @param {string} api - api route to application
 * @param {string} application - name of application
 * @param {string} service - name of service
 * @param {string} serviceApi - service api route
 * @param {object} body - parameter object of POST api
 */
export function servicePostApi(api, application, service, serviceApi, body) {
    return executeServiceApi(api, application, service, serviceApi, 'POST', body, {});
}

function getServiceOptions(data) {
    return {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'CSRF-Token': Cookies.get('csrfToken') || null,
            Accept: 'application/json'
        },
        body: JSON.stringify(data)
    };
}

function executeServiceApi(api, application, service, serviceApi, method, body, params) {
    return fetch(
        api,
        getServiceOptions({
            application: application,
            service: service,
            api: serviceApi,
            method: method,
            body: body,
            params: params
        })
    )
        .then(response => response.json())
        .then(r => {
            if (r.body && r.body.result) {
                return r.body.result;
            } else {
                return Q.reject(r.body);
            }
        });
}
