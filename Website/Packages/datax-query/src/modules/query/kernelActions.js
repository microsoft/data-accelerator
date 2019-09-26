// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import Q from 'q';
import * as Api from './api';
import { getApiErrorMessage } from 'datax-common';
import * as Selectors from './kernelSelectors';

// REDUX Action Types
export const KERNEL_UPDATE_KERNEL_VERSION = 'KERNEL_UPDATE_KERNEL_VERSION';
export const KERNEL_UPDATE_KERNEL = 'KERNEL_UPDATE_KERNEL';
export const KERNEL_FETCHING_KERNEL = 'KERNEL_FETCHING_KERNEL';

export function fetchingKernel(dispatch, value) {
    return dispatch({
        type: KERNEL_FETCHING_KERNEL,
        value: value
    });
}

export function updateKernel(dispatch, kernelId, version, warning) {
    return dispatch({
        type: KERNEL_UPDATE_KERNEL,
        kernelId: kernelId,
        version: version,
        warning: warning
    });
}

export const updateKernelVersion = version => dispatch => {
    return dispatch({
        type: KERNEL_UPDATE_KERNEL_VERSION,
        version: version
    });
};

export const getKernel = (queryMetadata, version, updateErrorMessage) => (dispatch, getState) => {
    updateErrorMessage(dispatch, undefined);
    fetchingKernel(dispatch, true);
    return Api.getDiagnosticKernel(queryMetadata)
        .then(response => {
            const kernelId = response.result;
            const warning = response.message;

            const curVersion = Selectors.getKernelVersion(getState());

            if (version >= curVersion) {
                return updateKernel(dispatch, kernelId, version, warning);
            } else {
                return Api.deleteDiagnosticKernel(kernelId, queryMetadata.name);
            }
        })
        .catch(error => {
            const message = getApiErrorMessage(error);
            updateErrorMessage(dispatch, message);
            fetchingKernel(dispatch, false);
            return Q.reject({ error: true, message: message });
        });
};

export const refreshKernel = (queryMetadata, kernelId, version, updateErrorMessage) => (dispatch, getState) => {
    updateErrorMessage(dispatch, undefined);
    fetchingKernel(dispatch, true);
    return Api.refreshDiagnosticKernel(queryMetadata, kernelId)
        .then(response => {
            const kernelId = response.result;
            const warning = response.message;

            const curVersion = Selectors.getKernelVersion(getState());

            if (version >= curVersion) {
                return updateKernel(dispatch, kernelId, version, warning);
            } else {
                return Api.deleteDiagnosticKernel(kernelId, queryMetadata.name);
            }
        })
        .catch(error => {
            const message = getApiErrorMessage(error);
            updateErrorMessage(dispatch, message);
            fetchingKernel(dispatch, false);
            return Q.reject({ error: true, message: message });
        });
};

export const deleteAllKernels = (updateErrorMessage, flowName) => dispatch => {
    updateErrorMessage(dispatch, undefined);
    return Api.deleteAllKernels(flowName)
        .then(result => {
            return result;
        })
        .catch(error => {
            const message = getApiErrorMessage(error);
            updateErrorMessage(dispatch, message);
            return Q.reject({ error: true, message: message });
        });
};

export const deleteKernel = (kernelId, version, flowName) => dispatch => {
    return Api.deleteDiagnosticKernel(kernelId, flowName)
        .then(result => {
            return updateKernel(dispatch, '', version, undefined);
        })
        .catch(error => {
            const message = getApiErrorMessage(error);
            return Q.reject({ error: true, message: message });
        });
};
