// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
const mongo = require('./mongoclient');
const config = require('../config');
const Q = require('q');

function reject(msg) {
    return Q.reject({ message: msg });
}

function handleTranction(resultChecker) {
    return r => {
        if (!r.result || !r.result.ok) return reject('db transaction failed:' + JSON.stringify(r));
        else {
            let check = resultChecker(r);
            return typeof check === 'string' ? reject(check) : Object.assign({ resultCode: 0 }, check);
        }
    };
}

function getOneEntityHandler(collectionName, query) {
    return results => {
        if (!results) return reject(`Something wrong happened when querying ${collectionName}, result is empty`);
        else if (results.length === 1) return Q.resolve(results[0]);
        else if (results.length === 0) return reject(`doc with '${query}' is not found in collection '${collectionName}'.`);
        else return reject(`docs with '${query}' are more than one in collection '${collectionName}'.`);
    };
}

const primaryDb = mongo(config.env.mongoDbUrl);
const sharedDb = mongo(config.env.mongoSharedDbUrl);

const envScope = config.env.name;
const filterByEnvScope = results => results.filter(r => !r.scope || r.scope.some(s => s == envScope));

function collection(collectionName, db) {
    return {
        findAll: () => db.findAll(collectionName).then(filterByEnvScope),
        findActives: () => db.findActives(collectionName).then(filterByEnvScope),
        new: entity =>
            db
                .insert(collectionName, entity)
                .then(
                    handleTranction(r =>
                        r.insertedCount === 1
                            ? { inserted: r.ops[0] }
                            : `Number of inserted documents is not exactly one, actually number is: ${r.insertedCount}`
                    )
                ),
        update: (id, entity) =>
            db
                .update(collectionName, id, entity)
                .then(
                    handleTranction(r =>
                        r.result.n === 1
                            ? { updated: entity }
                            : `Number of updated documents is not exactly one, actually number is: ${r.result.n}`
                    )
                ),

        upsertByName: entity =>
            db
                .upsertByName(collectionName, entity)
                .then(
                    handleTranction(r =>
                        r.result.n === 1
                            ? { updated: entity }
                            : `Number of updated documents is not exactly one, actually number is: ${r.result.n}`
                    )
                ),

        partialUpdate: (id, entity) => db.partialUpdate(collectionName, id, entity),
        delete: id =>
            db
                .deleteOne(collectionName, id)
                .then(
                    handleTranction(r =>
                        r.deletedCount === 1 ? {} : `Number of deleted documents is not exactly one, actually number is: ${r.deletedCount}`
                    )
                ),
        getOneById: id => db.findById(collectionName, id).then(getOneEntityHandler(collectionName, `id: ${id}`)),
        getOneByName: name => db.findByName(collectionName, name).then(getOneEntityHandler(collectionName, `name: ${name}`)),
        getByNames: names => db.findByNames(collectionName, names),
        getByFieldValues: (field, values) => db.findByFieldValues(collectionName, field, values),
        ensureNoEntityWithName: name =>
            db.findByName(collectionName, name).then(results => {
                if (!results) return reject(`Something wrong happened when querying ${collectionName}, result is empty`);
                else if (results.length === 0) return Q.resolve();
                else return reject(`docs with name '${name}' have ${results.length} in collection '${collectionName}', expected 0.`);
            })
    };
}

const Collection = {
    Dataflows: collection('dataflows', primaryDb),
    Pipes: collection('pipes', primaryDb),
    Pipings: collection('pipings', primaryDb),
    Streams: collection('streams', primaryDb),
    Processors: collection('processors', primaryDb),
    AzureStorageAccounts: collection('azureStorages', sharedDb),
    SparkJobs: collection('sparkJobs', sharedDb),
    SparkJobTemplates: collection('sparkJobTemplates', sharedDb),
    SparkClusters: collection('sparkClusters', sharedDb),
    SqlServers: collection('sqlServers', sharedDb),
    Products: collection('products', primaryDb),
    MetricWidgets: collection('metricWidgets', primaryDb),
    MetricSources: collection('metricSources', primaryDb),
    CommonSettings: collection('commons', primaryDb)
};

function upsertDataflow(df) {
    if (!df) return reject('content cannot be empty');
    if (!df.input) return reject('input cannot be empty');
    if (df.process && !df.output) return reject('output cannot be empty while there is process');
    if (!df.process && (!df.sinkers || df.sinkers.length == 0)) return reject('sinkers cannot be empty while there is no process');

    let validations = [];
    let transactions = [];

    const Streams = Collection.Streams;
    function handleStreamSetting(settingName) {
        let stream = df[settingName];
        if (stream) {
            if (typeof stream === 'string') {
                validations.push(() =>
                    Streams.getOneByName(stream).then(s => {
                        if (settingName == 'input') {
                            if (s.properties && s.properties.filterType) {
                                if (!df.filter) return Q.reject('No filter is specified when the input stream requires a filter.');
                                if (df.filter.type != s.properties.filterType)
                                    return Q.reject(
                                        `Filter type mismatch, expected ${s.properties.filterType}, actually it is ${df.filter.type}`
                                    );
                            } else if (df.filter) {
                                return Q.reject(
                                    'Unexpected filter is specified while there is no requirement on filters for the input stream.'
                                );
                            }
                        }
                    })
                );
            } else {
                if (stream._id) {
                    validations.push(() => Streams.getOneById(stream._id));
                    transactions.push(df =>
                        Streams.update(stream._id, stream).then(r => {
                            df[settingName] = r.updated.name;
                            return df;
                        })
                    );
                } else {
                    validations.push(() => Streams.ensureNoEntityWithName(stream.name));
                    transactions.push(df =>
                        Streams.new(stream).then(r => {
                            df[settingName] = r.inserted.name;
                            return df;
                        })
                    );
                }
            }
        }
    }

    // input
    handleStreamSetting('input');

    if (df.process) {
        validations.push(() =>
            Collection.Processors.getOneByName(df.process.type).then(r => {
                if (r.properties && r.properties.filterType) {
                    if (!df.output) return Q.reject('output should not be null while there is process');
                    if (!df.output.properties) df.output.properties = {};
                    df.output.properties.filterType = r.properties.filterType;
                }
            })
        );
    }

    // output
    handleStreamSetting('output');

    if (df._id) transactions.push(df => Collection.Dataflows.updateEntity(df._id, df));
    else transactions.push(df => Collection.Dataflows.new(df));

    return Q.all(validations.map(_ => _())).then(() => transactions.reduce((t, f) => t.then(f), Q(df)));
}

module.exports = {
    getStreams: () => Collection.Streams.findAll(),

    getPipes: () =>
        Collection.Pipes.findAll().then(results =>
            results.map(r => {
                delete r.reader;
                delete r.writer;
                return r;
            })
        ),

    getPipings: () => Collection.Pipings.findAll(),

    getProcessors: () => Collection.Processors.findAll(),
    getDataflows: () => Collection.Dataflows.findAll(),
    getDataflowByName: name => Collection.Dataflows.getOneByName(name),
    getDataflowById: id => Collection.Dataflows.getOneById(id),
    getStreamByName: name => Collection.Streams.getOneByName(name),
    getPipeByName: name => Collection.Pipes.getOneByName(name),
    upsertDataflow: upsertDataflow,
    updateDataflow: (id, content) => Collection.Dataflows.partialUpdate(id, content),
    deleteDataflow: id => Collection.Dataflows.delete(id),

    getAzureAccounts: () => Collection.AzureStorageAccounts.findAll(),
    getSqlServers: () => Collection.SqlServers.findAll(),

    getSparkJobs: () => Collection.SparkJobs.findAll(),
    getSparkJobById: id => Collection.SparkJobs.getOneById(id),
    getSparkJobByNames: names => Collection.SparkJobs.getByNames(names),
    getSparkClusterByName: name => Collection.SparkClusters.getOneByName(name),
    updateSparkJobState: (id, state, batch) => Collection.SparkJobs.partialUpdate(id, { state: state, batch: batch }),
    upsertSparkJob: job => Collection.SparkJobs.upsertByName(job),

    getSparkJobTemplateByName: name => Collection.SparkJobTemplates.getOneByName(name),

    getProducts: () => Collection.Products.findAll(),
    getActiveProducts: () => Collection.Products.findActives(),
    getProductByName: name => Collection.Products.getOneByName(name),
    putProduct: product => Collection.Products.new(product),
    updateProductJobNames: (id, names) => Collection.Products.partialUpdate(id, { jobNames: names }),
    updateProductMetrics: (id, metrics) =>
        Collection.Products.partialUpdate(id, { 'metrics.sources': metrics.sources, 'metrics.widgets': metrics.widgets }),
    getCommonProcessorMetricWidgetsBySets: sets => Collection.MetricWidgets.getByFieldValues('set', sets),
    getCommonProcessorMetricSourcesByNames: names => Collection.MetricSources.getByNames(names),

    getCommonSettings: () => Collection.CommonSettings.findAll(),
    getCommonSettingByName: name => Collection.CommonSettings.getOneByName(name)
};
