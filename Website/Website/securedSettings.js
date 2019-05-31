// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
const kvclient = require('./db/keyvaultClient');
const logger = require('./util/consolelogger');

function waterfall(promises) {
    if (promises.length == 0) {
        return Promise.resolve();
    } else {
        var lastPromise = null;
        promises.forEach(p => {
            if (lastPromise == null) lastPromise = p();
            else lastPromise = lastPromise.then(p);
        });

        return lastPromise;
    }
}

module.exports = async function(host) {
    const env = host.conf.env;
    const kv = kvclient(env);
    host.keyvault = kv;

    const kvPrefix = env.kvPrefix;

    const getSecretOrThrow = name =>
        kv.getSecretOrThrow(kvPrefix + name).then(v => {
            logger.info(`Retrieved setting ${name}.`);
            return v;
        });

    const getSecret = name =>
        getSecretOrThrow(name).catch(err => {
            logger.info(`Failed to retrieve setting ${name}.`);
            return null;
        });

    const setProperty = name => () => {
        if (!env[name]) return getSecretOrThrow(name).then(v => (env[name] = v));
        else return Promise.resolve();
    };

    await waterfall(
        ['aiKey', 'subscriptionId', 'sessionSecret', 'tenantName', 'serviceResourceId', 'serviceClusterUrl', 'clientId', 'clientSecret']
            .map(setProperty)
            .concat([
                async function() {
                    env.mongoDbUrl = await getSecretOrThrow('mongoDbUrl');
                    env.mongoSharedDbUrl = await getSecretOrThrow('mongoSharedDbUrl').catch(err => env.mongoDbUrl);
                },
                ,
                async function() {
                    env.kubernetesServices = await getSecret('kubernetesServices');
                },
                async function() {
                    let redisDataConnectionString = await getSecret('redisDataConnectionString');
                    if (redisDataConnectionString)
                        env.redisData = {
                            server: redisDataConnectionString
                        };
                },
                async function() {
                    let uploadStorageConnectionString = await getSecret('uploadStorageConnectionString');
                    env.uploadsRepo = {
                        storage: uploadStorageConnectionString ? 'azure' : null,
                        azure: {
                            connectionString: uploadStorageConnectionString,
                            container: 'dataflow'
                        },
                        baseFolder: 'prod/uploads'
                    };

                    // Common settings
                    if (env.uploadsRepo.storage == 'disk') {
                        env.uploadFolder = __dirname + '/uploads';
                        fs.ensureFolderExists(env.uploadFolder);
                    }
                }
            ])
    );

    logger.info('secured settings are all initialized.');

    return env;
};
