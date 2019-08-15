// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

import './styles/styles.css';
import 'datax-query/dist/css/index.css'; 

import flowslist from './modules/flowList/reducer';
import flow from './modules/flowDefinition/flowReducer';
import { kernelReducer, layoutReducer, queryReducer } from 'datax-query';

/**
 * Website Consumption Contract
 *
 * 1) Export all React components that you want to support being dynamically loaded by website.
 *    a) The name you give to the exported component is important as its the name the website will use
 *       to pick out your component from this package.
 *
 *    b) It is best practice to capitalize your component name during the export.
 *
 * 2) Export all REDUX reducers as single object.
 *    a) The name of your reducers must be "reducers".
 *       Example:
 *       export const reducers = ...
 *
 *    b) If you don't have reducers, return empty object {}.
 *       Example:
 *       export const reducers = {};
 *
 *    c) The name you give to represent your individual reducer is important. That given name is the name of how
 *       you will access it from the state REDUX object.
 *       i.e. Giving name apple will be exposed as state.apple and giving name Apple will be exposed as state.Apple
 *       For more information, please see REDUX documentation: https://redux.js.org/
 *
 *       Example 1: Name exposed to state and reducer is the same
 *       export const reducers = { apple, orange }; // equivalent to writing "{ apple: apple, orange: orange }"
 *
 *       Example 2: Name exposed to state is not same as reducer name
 *       export const reducers = { apple: appleReducer, orange: orangeReducer };
 */

// Exported REDUX Reducers
export const reducers = {
    flowslist,
    flow,
    queryKernel: kernelReducer,
    queryLayoutSettings: layoutReducer,
    query: queryReducer
};

// Exported React Components
export { default as FlowListPanel } from './modules/flowList/components/flowListPanel';
export { default as FlowDefinitionPanel } from './modules/flowDefinition/components/flowDefinitionPanel';
