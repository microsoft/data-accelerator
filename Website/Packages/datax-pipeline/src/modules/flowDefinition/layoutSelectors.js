// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import { createSelector } from 'reselect';

export const getLayoutSettings = state => state.flowLayoutSettings;

export const getTestQueryOutputPanelVisibility = createSelector(
    getLayoutSettings,
    flowLayoutSettings => flowLayoutSettings.isTestQueryOutputPanelVisible
);
