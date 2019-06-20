// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import { createSelector } from 'reselect';

// Settings - query
export const getQuery = state => state.query;

// Settings - Query
export const getQueryContent = createSelector(
    getQuery,
    query => query.query
);

// Validation - Query
export const getQueryDirty = createSelector(
    getQuery,
    query => query.isDirty
);

// Validation - Query
export const validateQueryTab = createSelector(
    getQueryContent,
    validateQuery
);

function validateQuery(query) {
    return query? (query || query.trim() === ''): true;
}
