// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import { createStore, combineReducers, applyMiddleware } from 'redux';
import thunk from 'redux-thunk';
import { userReducer } from 'datax-common';

export const storeManager = {
    store: null,
    dynamicReducerMap: {},

    createStore() {
        this.store = createStore(this.createRootReducer(), applyMiddleware(thunk));
        return this.store;
    },

    registerReducers(reducerMap) {
        let newReducers = false;

        // Check if we have any new reducers to register
        Object.entries(reducerMap).forEach(([name, reducer]) => {
            if (!(name in this.dynamicReducerMap)) {
                this.dynamicReducerMap[name] = reducer;
                newReducers = true;
            }
        });

        if (newReducers) {
            // Refresh the store to contain all the reducers we have registered so far
            this.store.replaceReducer(this.createRootReducer());
        }
    },

    createRootReducer() {
        return combineReducers({
            // Declare all static reducers here.
            // If static reducers do not exist then add "root" example below in place of no static reducers case
            //      root: (state = null) => state,
            userInfo: userReducer,

            // Dynamic reducers
            ...this.dynamicReducerMap
        });
    }
};

export default storeManager;
