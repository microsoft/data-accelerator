// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import Q from 'q';
import { serviceGetApi, servicePostApi, nodeServiceGetApi, Constants, ApiNames} from 'datax-common';

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
