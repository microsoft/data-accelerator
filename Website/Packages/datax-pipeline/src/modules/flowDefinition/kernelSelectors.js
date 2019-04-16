// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import { createSelector } from 'reselect';

export const getKernelInfo = state => state.flowKernel;

export const getKernelId = createSelector(
    getKernelInfo,
    flowKernel => flowKernel.kernelId
);

export const getKernelVersion = createSelector(
    getKernelInfo,
    flowKernel => flowKernel.version
);

export const getFetchingKernel = createSelector(
    getKernelInfo,
    flowKernel => flowKernel.fetchingKernel
);
