// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import { serviceGetApi, servicePostApi, nodeServiceGetApi } from 'datax-common';
import { Constants } from './constants';

export const listSparkJobs = () =>
    serviceGetApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.flow, 'job/getall');

export const syncSparkJobs = () =>
    serviceGetApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.flow, 'job/syncall');

export const stopAllSparkJobs = () =>
    serviceGetApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.flow, 'job/stopall');

export const startAllSparkJobs = () =>
    serviceGetApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.flow, 'job/startall');

export const startSparkJob = name =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.flow, 'job/start', name);

export const stopSparkJob = name =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.flow, 'job/stop', name);

export const restartSparkJob = name =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.flow, 'job/restart', name);

export const functionEnabled = () => nodeServiceGetApi('functionenabled');
