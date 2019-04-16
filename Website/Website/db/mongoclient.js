// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
const MongoClient = require('mongodb').MongoClient;
const ObjectId = require('mongodb').ObjectID;
const log = require('../logger');
const Q = require('q');
const telemetryClient = require('applicationinsights').defaultClient;

function connect(url) {
    var defer = Q.defer();
    if (url == null) {
        //handle cases where config is not fully set up on dev machine
        console.error('MongoDB environment not set properly');
        return;
    }

    function connectWithRetry() {
        MongoClient.connect(
            url,
            {
                reconnectTries: 60,
                reconnectInterval: 1000,
                autoReconnect: true
            },
            function(err, database) {
                if (err) {
                    log.error('Failed to connect to mongo on startup - retrying in 5 sec:' + err);
                    telemetryClient.trackException({ exception: err });
                    setTimeout(connectWithRetry, 5000);
                    //defer.reject(err);
                } else {
                    log.info(`Connected successfully to mongo db '${database.databaseName}'`);
                    defer.resolve(database);
                }
            }
        );
    }

    connectWithRetry();

    return defer.promise;
}

function replaceInAllKeys(obj, search, replace) {
    if (Array.isArray(obj)) {
        obj.forEach(o => replaceInAllKeys(o, search, replace));
    } else if (typeof obj === 'object' && obj !== null) {
        Object.keys(obj).forEach(k => {
            obj[k] = replaceInAllKeys(obj[k], search, replace);
            if (search.test(k)) {
                let newKey = k.replace(search, replace);
                obj[newKey] = obj[k];
                delete obj[k];
            }
        });
    }

    return obj;
}

function replaceDotInPartialUpdate(partial) {
    let obj = partial;
    Object.keys(obj).forEach(k => {
        obj[k] = replaceDotInKeys(obj[k]);
    });

    return obj;
}

function replaceDotInKeys(obj) {
    return replaceInAllKeys(obj, /\./g, '\uff0e');
}

function restoreDotInKeys(obj) {
    return replaceInAllKeys(obj, /\uff0e/g, '.');
}

module.exports = function factory(url) {
    var connection = connect(url);

    function query(collection, filter) {
        return connection.then(db =>
            Q.Promise(function(resolve, reject) {
                db.collection(collection)
                    .find(filter)
                    .toArray(function(err, docs) {
                        if (err) reject(err);
                        else resolve(restoreDotInKeys(docs));
                    });
            })
        );
    }

    function modifyOperation(collection, operation) {
        return connection.then(db =>
            Q.Promise((resolve, reject) => {
                operation(db.collection(collection), (err, result) => {
                    if (err) reject(err);
                    else resolve(result);
                });
            })
        );
    }

    return {
        findAll: function(collection) {
            return query(collection, {});
        },
        query: query,
        findById: (collection, id) => query(collection, { _id: ObjectId(id) }),
        findByName: (collection, name) => query(collection, { name: name }),
        findByNames: (collection, names) => query(collection, { name: { $in: names } }),
        findActives: collection => query(collection, { disabled: { $ne: true } }),
        findByFieldValues: (collection, field, values) => {
            let queryExpr = {};
            queryExpr[field] = { $in: values };
            return query(collection, queryExpr);
        },
        insert: (collection, doc) =>
            modifyOperation(collection, (docs, resultHandler) => {
                docs.insertOne(replaceDotInKeys(doc), resultHandler);
            }),
        deleteOne: (collection, id) =>
            modifyOperation(collection, (docs, resultHandler) => {
                docs.deleteOne({ _id: ObjectId(id) }, resultHandler);
            }),
        update: (collection, id, doc) =>
            modifyOperation(collection, (docs, resultHandler) => {
                delete doc._id;
                docs.update({ _id: ObjectId(id) }, replaceDotInKeys(doc), {}, resultHandler);
            }),
        upsertByName: (collection, doc) =>
            modifyOperation(collection, (docs, resultHandler) => {
                docs.update({ name: doc.name }, { $set: replaceDotInPartialUpdate(doc) }, { upsert: true }, resultHandler);
            }),
        partialUpdate: (collection, id, partial) =>
            modifyOperation(collection, (docs, resultHandler) => {
                docs.update({ _id: ObjectId(id) }, { $set: replaceDotInPartialUpdate(partial) }, {}, resultHandler);
            })
    };
};
