// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import { createSelector } from 'reselect';

export const getMetricProducts = state => state.metricProducts;

export const getMetricProductItems = createSelector(
    getMetricProducts,
    metricProducts => metricProducts.items
);
