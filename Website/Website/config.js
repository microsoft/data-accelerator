// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
const fs = require('./util/fs');
const webComposition = require('./web.composition.json');
/**
 * Initialize 'use local services' settings
 */
function initializeLocalServices() {
    let localServices = {};
    Object.keys(webComposition.localServices).forEach(key => {
        // Set local services to null if the corresponding 'use local service' environment variables is not defined.
        // If defined then keep track its assigned 'http://localhost:port#' URL which is used to target locally hosted services.
        localServices[key] = process.env[webComposition.localServices[key]];
    });
    return localServices;
}

module.exports = (function() {
    var envName = process.env.NODE_ENV || 'dev';
    var keyVaultName = process.env.DATAX_KEYVAULT_NAME;
    var keyVaultSecretNamePrefix =
        process.env.DATAX_KEYVAULT_SECRET_PREFIX !== undefined ? process.env.DATAX_KEYVAULT_SECRET_PREFIX : 'dataxweb-dev-';

    if (keyVaultName === undefined && process.env.DATAX_ENABLE_ONEBOX !== 'true') {
        console.error('!!env variable DATAX_KEYVAULT_NAME is not defined.');
        process.exit(1);
    }

    var websiteHostName = process.env.WEBSITE_HOSTNAME;
    var defaultKeyVaultPrefix = `keyvault://${keyVaultName}/${keyVaultSecretNamePrefix}`;

    var dynamics = {
        // Set NODE_ENV to 'production' if your website and its web server is deployed and hosted on Azure as an App Service.
        production: {
            dist: 'prod',
            kvPrefix: defaultKeyVaultPrefix,
            isMSISupported: true
        },
        // Set NODE_ENV to 'dev' if you are locally developing and hosting the website and its web server on your local machine.
        dev: {
            port: 2020,
            dist: 'dev',
            kvPrefix: defaultKeyVaultPrefix,
            authReturnUrl: 'http://localhost:2020/authReturn',
            isMSISupported: false,
            isLocal: true // Whether the web server is hosted locally
        }
    };

    if (!(envName in dynamics)) {
        console.error("Could not load config for environment '" + env + "'");
    }

    var env = dynamics[envName];
    env.name = envName;
    env.aiKey = process.env.APPINSIGHTS_INSTRUMENTATIONKEY;
    env.authReturnUrl = process.env.returnUrl || (websiteHostName ? `https://${websiteHostName}/authReturn` : env.authReturnUrl);
    env.port = process.env.port ? process.env.port : env.port;
    env.keyvaultMode = process.env.DATAXDEV_KEYVAULTMODE || 'local,remote';
    env.localServices = initializeLocalServices();

    // This is used to support usage of self signed SSL certs by the services.
    env.rejectUnauthorized = process.env.DATAXDEV_CERTTYPE === undefined ? true : process.env.DATAXDEV_CERTTYPE.toLowerCase() !== 'test';

    env.enableLocalOneBox = Boolean(
        process.env.DATAX_ENABLE_ONEBOX && (process.env.DATAX_ENABLE_ONEBOX === 'true' || process.env.DATAX_ENABLE_ONEBOX === 'TRUE')
    );

    const conf = {
        app: { name: 'datax' },
        env: env,
        dynamics: dynamics
    };

    conf.import = async source => {
        await source(conf.env);
        return conf;
    };

    return conf;
})();
