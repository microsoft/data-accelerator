// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import { Constants, servicePostApi } from 'datax-common';

export const getTableSchemas = (flowname, displayName, query, inputSchemaFile, rules, outputTemplates) =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.flow, 'userqueries/schema', {
        name: flowname,
        displayName: displayName,
        query: query,
        inputSchema: inputSchemaFile,
        rules: rules,
        outputTemplates: outputTemplates
    });

export const getCodeGenQuery = (flowname, displayName, query, rules, outputTemplates) =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.flow, 'userqueries/codegen', {
        name: flowname,
        displayName: displayName,
        query: query,
        rules: rules,
        outputTemplates: outputTemplates
    });

// Interactive Query
export const getDiagnosticKernel = (flowname, displayName, owner, inputSchemaFile, normalizationSnippet, referenceData, functions) =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.interactiveQuery, 'kernel', {
        name: flowname,
        displayName: displayName,
        userName: owner,
        inputSchema: inputSchemaFile,
        normalizationSnippet: normalizationSnippet,
        refData: referenceData,
        functions: functions
    });

export const refreshDiagnosticKernel = (flowname, displayName, owner, inputSchemaFile, normalizationSnippet, referenceData, functions, kernelId) =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.interactiveQuery, 'kernel/refresh', {
        kernelId: kernelId,
        userName: owner,
        name: flowname,
        displayName: displayName,
        inputSchema: inputSchemaFile,
        normalizationSnippet: normalizationSnippet,
        refData: referenceData,
        functions: functions
    });

export const deleteAllKernels = () =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.interactiveQuery, 'kernels/deleteall', {});

export const deleteDiagnosticKernelOnUnload = kernelId =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.interactiveQuery, 'kernel/delete', kernelId);

export const deleteDiagnosticKernel = deleteDiagnosticKernelOnUnload;

export const executeQuery = (flowname, displayName, rules, outputTemplates, selectedQuery, kernelId) =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.interactiveQuery, 'kernel/executequery', {
        name: flowname,
        displayName: displayName,
        query: selectedQuery,
        kernelId: kernelId,
        rules: rules,
        outputTemplates: outputTemplates
    });

// Live Data
export const resampleInput = (flowname, displayName, owner, inputSchemaFile, normalizationSnippet, inputEventhubConnection, inputSubscriptionId, inputResourceGroup, inputEventhubName, inputType, resamplingInputDuration, kernelId) =>
    servicePostApi(
        Constants.serviceRouteApi,
        Constants.serviceApplication,
        Constants.services.liveData,
        'inputdata/refreshsampleandkernel',
        {
            name: flowname,
            displayName: displayName,
            userName: owner,
            kernelId: kernelId,
            inputSchema: inputSchemaFile,
            normalizationSnippet: normalizationSnippet,
            eventhubConnectionString: inputEventhubConnection,
            inputSubscriptionId: inputSubscriptionId,
            inputResourceGroup: inputResourceGroup,
            eventHubNames: inputEventhubName,
            inputType: inputType,
            seconds: resamplingInputDuration
        }
    );