// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import { createSelector } from 'reselect';

// Settings - Flow
export const getQuery = state => state.query;

// Settings - Query
export const getFlowQuery = createSelector(
    getQuery,
    query => query.query
);


// Validation - Query
export const validateFlowQuery = createSelector(
    getFlowQuery,
    validateQuery
);

function validateQuery(query) {
    //removing validation; codegen will add OUTPUTs for alerts. Blank query is valid.
    return query || query.trim() === '';
}
