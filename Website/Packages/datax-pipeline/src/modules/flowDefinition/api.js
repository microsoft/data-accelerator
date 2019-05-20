// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import Q from 'q';
import { serviceGetApi, servicePostApi, nodeServiceGetApi } from 'datax-common';
import { Constants, ApiNames } from '../../common/apiConstants';
import * as Models from './flowModels';
import * as Helpers from './flowHelpers';

// Flow Service
export const getFlow = name => getProduct(name).then(f => f.gui);

export const saveFlow = config =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.flow, 'flow/save', config);

export const deleteFlow = flow =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.flow, 'flow/delete', {
        subscription: flow.subscription,
        name: flow.name,
        displayName: flow.displayName,
        userName: flow.owner,
        eventhubConnectionString: flow.input.properties.inputEventhubConnection,
        inputSubscriptionId: flow.input.properties.inputSubscriptionId,
        inputResourceGroup: flow.input.properties.inputResourceGroup,
        eventHubNames: flow.input.properties.inputEventhubName,
        inputType: flow.input.type,
    });

export const getTableSchemas = flow =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.flow, 'userqueries/schema', {
        name: flow.name,
        displayName: flow.displayName,
        query: flow.query,
        inputSchema: flow.input.properties.inputSchemaFile,
        rules: Helpers.convertFlowToConfigRules(flow.rules),
        outputTemplates: flow.outputTemplates
    });

export const getCodeGenQuery = flow =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.flow, 'userqueries/codegen', {
        name: flow.name,
        displayName: flow.displayName,
        query: flow.query,
        rules: Helpers.convertFlowToConfigRules(flow.rules),
        outputTemplates: flow.outputTemplates
    });

// Interactive Query
export const getDiagnosticKernel = flow =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.interactiveQuery, 'kernel', {
        name: flow.name,
        displayName: flow.displayName,
        userName: flow.owner,
        inputSchema: flow.input.properties.inputSchemaFile,
        normalizationSnippet: flow.input.properties.normalizationSnippet,
        refData: flow.referenceData,
        functions: flow.functions
    });

export const refreshDiagnosticKernel = (flow, kernelId) =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.interactiveQuery, 'kernel/refresh', {
        kernelId: kernelId,
        userName: flow.owner,
        name: flow.name,
        displayName: flow.displayName,
        inputSchema: flow.input.properties.inputSchemaFile,
        normalizationSnippet: flow.input.properties.normalizationSnippet,
        refData: flow.referenceData,
        functions: flow.functions
    });

export const deleteAllKernels = () =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.interactiveQuery, 'kernels/deleteall', {});

export const deleteDiagnosticKernelOnUnload = kernelId =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.interactiveQuery, 'kernel/delete', kernelId);

export const deleteDiagnosticKernel = deleteDiagnosticKernelOnUnload;

export const executeQuery = (flow, selectedQuery, kernelId) =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.interactiveQuery, 'kernel/executequery', {
        name: flow.name,
        displayName: flow.displayName,
        query: selectedQuery,
        kernelId: kernelId,
        rules: Helpers.convertFlowToConfigRules(flow.rules),
        outputTemplates: flow.outputTemplates
    });

// Schema Inference
export const getInputSchema = flow =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.schemaInference, 'inputdata/inferschema', {
        name: flow.name,
        displayName: flow.displayName,
        userName: flow.owner,
        eventhubConnectionString: flow.input.properties.inputEventhubConnection,
        inputSubscriptionId: flow.input.properties.inputSubscriptionId,
        inputResourceGroup: flow.input.properties.inputResourceGroup,
        eventHubNames: flow.input.properties.inputEventhubName,
        inputType: flow.input.type,
        seconds: flow.samplingInputDuration
    });

// Live Data
export const resampleInput = (flow, kernelId) =>
    servicePostApi(
        Constants.serviceRouteApi,
        Constants.serviceApplication,
        Constants.services.liveData,
        'inputdata/refreshsampleandkernel',
        {
            name: flow.name,
            displayName: flow.displayName,
            userName: flow.owner,
            kernelId: kernelId,
            inputSchema: flow.input.properties.inputSchemaFile,
            normalizationSnippet: flow.input.properties.normalizationSnippet,
            eventhubConnectionString: flow.input.properties.inputEventhubConnection,
            inputSubscriptionId: flow.input.properties.inputSubscriptionId,
            inputResourceGroup: flow.input.properties.inputResourceGroup,
            eventHubNames: flow.input.properties.inputEventhubName,
            inputType: flow.input.type,
            seconds: flow.resamplingInputDuration
        }
    );

// Product and Jobs

export const getProduct = name =>
    serviceGetApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.flow, 'flow/get', { flowName: name });

export const generateProductConfigs = name =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.flow, 'flow/generateconfigs', name);

export const listSparkJobsByNames = names =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.flow, 'job/getbynames', names);

export const restartSparkJob = name =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.flow, 'job/restart', name);

export const restartAllJobsForProduct = name =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.flow, 'flow/restartjobs', name);

// OneBox
export const initOneBoxMode = () => nodeServiceGetApi(ApiNames.EnableLocalOneBox);
