// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import { Constants, servicePostApi } from 'datax-common';

export const getTableSchemas = queryMetadata =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.flow, 'userqueries/schema', {
        name: queryMetadata.name,
        displayName: queryMetadata.displayName,
        query: queryMetadata.query,
        inputSchema: queryMetadata.inputSchema,
        rules: queryMetadata.rules,
        outputTemplates: queryMetadata.outputTemplates
    });

export const getCodeGenQuery = queryMetadata =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.flow, 'userqueries/codegen', {
        name: queryMetadata.name,
        displayName: queryMetadata.displayName,
        query: queryMetadata.query,
        rules: queryMetadata.rules,
        outputTemplates: queryMetadata.outputTemplates
    });

// Interactive Query
export const getDiagnosticKernel = queryMetadata =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.interactiveQuery, 'kernel', {
        name: queryMetadata.name,
        displayName: queryMetadata.displayName,
        userName: queryMetadata.userName,
        inputSchema: queryMetadata.inputSchema,
        normalizationSnippet: queryMetadata.normalizationSnippet,
        refData: queryMetadata.refData,
        functions: queryMetadata.functions
    });

export const refreshDiagnosticKernel = (queryMetadata, kernelId) =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.interactiveQuery, 'kernel/refresh', {
        kernelId: kernelId,
        userName: queryMetadata.userName,
        name: queryMetadata.name,
        displayName: queryMetadata.displayName,
        inputSchema: queryMetadata.inputSchema,
        normalizationSnippet: queryMetadata.normalizationSnippet,
        refData: queryMetadata.refData,
        functions: queryMetadata.functions
    });

export const deleteAllKernels = flowName =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.interactiveQuery, 'kernels/deleteall', flowName);

export const deleteDiagnosticKernelOnUnload = (kernelId, flowName) =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.interactiveQuery, 'kernel/delete', {
        kernelId: kernelId,
        name: flowName
    });

export const deleteDiagnosticKernel = deleteDiagnosticKernelOnUnload;

export const executeQuery = (queryMetadata, selectedQuery, kernelId) =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.interactiveQuery, 'kernel/executequery', {
        name: queryMetadata.name,
        displayName: queryMetadata.displayName,
        query: selectedQuery,
        kernelId: kernelId,
        rules: queryMetadata.rules,
        outputTemplates: queryMetadata.outputTemplates
    });

// Live Data
export const resampleInput = (queryMetadata, kernelId) =>
    servicePostApi(
        Constants.serviceRouteApi,
        Constants.serviceApplication,
        Constants.services.liveData,
        'inputdata/refreshsampleandkernel',
        {
            name: queryMetadata.name,
            displayName: queryMetadata.displayName,
            userName: queryMetadata.userName,
            kernelId: kernelId,
            inputSchema: queryMetadata.inputSchema,
            normalizationSnippet: queryMetadata.normalizationSnippet,
            eventhubConnectionString: queryMetadata.eventhubConnection,
            inputSubscriptionId: queryMetadata.inputSubscriptionId,
            inputResourceGroup: queryMetadata.inputResourceGroup,
            eventHubNames: queryMetadata.eventhubNames,
            inputType: queryMetadata.inputType,
            seconds: queryMetadata.seconds
        }
    );
