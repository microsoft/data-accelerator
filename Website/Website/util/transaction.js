// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
const Q = require('q');

function noop() {}

function resultHandler(rollback, finalize) {
    return () => ({
        rollback: rollback || noop,
        finalize: finalize || noop
    });
}

const noopResult = { rollback: noop, finalize: noop };

function rollbackOrFulfill(results) {
    let states = results.reduce((rv, r) => {
        if (r.state in rv) rv[r.state].push(r);
        else rv[r.state] = [r];
        return rv;
    }, {});

    if (states.rejected) {
        if (states.fulfilled)
            return Q.all(states.fulfilled.map(r => r.value.rollback())).then(rbResults => Q.reject(states.rejected.map(r => r.reason)));
        else return Q.reject(states.rejected.map(r => r.reason));
    } else {
        return states.fulfilled.map(r => r.value);
    }
}

function group(promises) {
    return Q.allSettled(promises)
        .then(rollbackOrFulfill)
        .then(results => ({
            rollback: () => Q.all(results.map(r => r.rollback())),
            finalize: () => Q.all(results.map(r => r.finalize()))
        }));
}

function commitAll(promises) {
    return Q.allSettled(promises)
        .then(rollbackOrFulfill)
        .then(results => Q.all(results.map(r => r.finalize())));
}

function basket() {
    let operations = [];

    return {
        add: oper => operations.push(oper),
        commit: () => commitAll(operations),
        result: () => group(operations)
    };
}

basket.group = group;
basket.noopResult = noopResult;
basket.resultHandler = resultHandler;

module.exports = basket;
