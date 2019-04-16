// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import * as Api from './api';

export const METRICS_RECEIVE_PRODUCTS = 'METRICS_RECEIVE_PRODUCTS';

export function getMetricProducts() {
    return dispatch =>
        Api.getProducts().then(products => {
            products = products
                .filter(p => !p.disabled)
                .sort((a, b) => {
                    if (a.displayOrderScore) {
                        if (b.displayOrderScore) {
                            if (a.displayOrderScore == b.displayOrderScore) return a.displayName <= b.displayName ? -1 : 1;
                            else return a.displayOrderScore - b.displayOrderScore;
                        } else {
                            return -1;
                        }
                    } else {
                        if (b.displayOrderScore) {
                            return 1;
                        } else {
                            return a.displayName <= b.displayName ? -1 : 1;
                        }
                    }
                });

            dispatch({
                type: METRICS_RECEIVE_PRODUCTS,
                payload: products
            });
        });
}
