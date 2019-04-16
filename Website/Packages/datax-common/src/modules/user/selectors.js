// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import { createSelector } from 'reselect';

export const getUserInfo = state => state.userInfo;

export const getUserAlias = createSelector(
    getUserInfo,
    userInfo => (userInfo ? userInfo.id : '')
);
