// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import { createSelector } from 'reselect';

export const getFlowsList = state => state.flowslist;

export const getFlowItems = createSelector(
    getFlowsList,
    flowslist => flowslist.items
);

export const getErrorMessage = createSelector(
    getFlowsList,
    flowslist => flowslist.errorMessage
);
