// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import { createSelector } from 'reselect';

export const getKernelInfo = state => state.queryKernel;

export const getKernelId = createSelector(
    getKernelInfo,
    queryKernel => queryKernel.kernelId
);

export const getKernelVersion = createSelector(
    getKernelInfo,
    queryKernel => queryKernel.version
);

export const getFetchingKernel = createSelector(
    getKernelInfo,
    queryKernel => queryKernel.fetchingKernel
);
