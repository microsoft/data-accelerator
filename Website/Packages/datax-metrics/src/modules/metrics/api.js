// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import { nodeServiceGetApi, serviceGetApi, servicePostApi } from 'datax-common';
import { Constants } from './apiConstants';

const ApiNames = {
    MetricGetData: (metricName, startTime, endTime) => `metrics/get?m=${metricName}&s=${startTime}&e=${endTime}`,
    MetricGetDataFreshness: metricName => `metrics/${metricName}/freshness`
};

export const getMetricsData = (metricName, startTime, endTime) =>
    nodeServiceGetApi(ApiNames.MetricGetData(metricName, startTime, endTime)).then(r => r.map(JSON.parse));

export const getMetricsDataFreshness = metricName =>
    nodeServiceGetApi(ApiNames.MetricGetDataFreshness(metricName)).then(r => r.map(JSON.parse));

export const getProducts = () =>
    serviceGetApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.flow, 'flow/getall');

export const allJobsRunningForProduct = product =>
    syncSparkJobsByNames(product.jobNames).then(jobResults => jobResults.every(result => result.state === 'Running'));

const syncSparkJobsByNames = names =>
    servicePostApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.flow, 'job/syncbynames', names);
