// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
const KeyVault = require('azure-keyvault');
const msRestAzure = require('ms-rest-azure');
const logger = require('../logger');
const Q = require('q');

const secretIdRegex = /^keyvault:\/\/([a-zA-Z0-9-_]+)\/([a-zA-Z0-9-_]+)$/;

function parseSecretId(secretId) {
    var match = secretIdRegex.exec(secretId);
    if (match == null || match.length < 3) {
        throw new Error(`!!unexpected secret id: '${secretId}'`);
    }

    return {
        keyVaultUrl: `https://${match[1]}.vault.azure.net/`,
        secretName: match[2]
    };
}

const vaultNameRegx = /^https:\/\/([a-zA-Z0-9-_]+)\.vault\.azure\.net/i;
function getVaultName(url) {
    var match = vaultNameRegx.exec(url);
    return match[1];
}

const envKvPrefix = 'DATAXDEV_';
const baseVaultName = process.env.DATAX_KEYVAULT_NAME;
const baseVaultSecretPrefix =
    process.env.DATAX_KEYVAULT_SECRET_PREFIX !== undefined ? process.env.DATAX_KEYVAULT_SECRET_PREFIX : 'dataxweb-dev-';
function getEnvVarName(url, name) {
    var vaultName = getVaultName(url);
    if (vaultName == baseVaultName) {
        if (name.indexOf(baseVaultSecretPrefix) == 0) return envKvPrefix + name.substring(baseVaultSecretPrefix.length).toUpperCase();
        else return envKvPrefix + name.toUpperCase();
    } else {
        return envKvPrefix + vaultName.toUpperCase() + '_' + name.toUpperCase();
    }
}

async function initLocalKeyVault() {
    return {
        getSecret: (url, name, version) => {
            var varName = getEnvVarName(url, name);
            var value = process.env[varName];
            if (value === undefined || value === null) {
                return Promise.reject(`Env var '${varName}' doesn't exist!`);
            } else {
                return Promise.resolve({ value: value });
            }
        },

        setSecret: (url, name, value) => {
            process.env[getEnvVarName(url, name)] = value;
            return Promise.resolve(value);
        },

        deleteSecret: (url, name) => {
            process.env[getEnvVarName(url, name)] = undefined;
            return Promise.resolve();
        }
    };
}

function chainVaults(vault1, vault2) {
    return {
        getSecret: (url, name, version) => vault1.getSecret(url, name, version).catch(err => vault2.getSecret(url, name, version)),
        setSecret: vault2.setSecret,
        deleteSecret: vault2.deleteSecret
    };
}

async function initCompoundClient(vaults) {
    var vaults = await Promise.all(vaults.map(initiator => initiator()));
    if (vaults.length == 0) throw new Error('no keyvault can be initialized');

    var vault = vaults[0];
    for (let index = 1; index < vaults.length; index++) {
        vault = chainVaults(vault, vaults[index]);
    }

    return vault;
}

module.exports = function initialize(conf) {
    function getKeyVaultCredentials() {
        if (conf.isMSISupported) {
            return msRestAzure.loginWithAppServiceMSI({ resource: 'https://vault.azure.net' });
        } else {
            //TODO: automate this launching the local user's browser to finish authentication
            return msRestAzure.interactiveLogin();
        }
    }

    var keyvaultMode = conf.keyvaultMode || 'local';

    var vaultInitiators = {
        local: initLocalKeyVault,
        remote: () =>
            getKeyVaultCredentials().then(creds => {
                logger.info('initialize keyvault client');
                return new KeyVault.KeyVaultClient(creds);
            })
    };

    // determine keyvault client mode, either to read from local env vars or read them from remote keyvault
    // e.g. if keyvaultMode = 'local', only parse env variables,
    // if keyvaultMode = 'local,remote', then first try to read it from local variables
    // then read from remote keyvault if failed.
    var vaultModes = {};
    keyvaultMode.split(',').map(m => {
        vaultModes[m] = true;
    });

    let deferredClients = [];
    let vaultClient = null;
    initCompoundClient(Object.keys(vaultModes).map(m => vaultInitiators[m])).then(client => {
        vaultClient = client;
        deferredClients.forEach(d => {
            d.resolve(vaultClient);
        });

        return vaultClient;
    });

    function getClient() {
        if (vaultClient) return Promise.resolve(vaultClient);
        else {
            var deferred = Q.defer();
            deferredClients.push(deferred);
            return deferred.promise;
        }
    }

    function getSecret(secretId) {
        var secretInfo = parseSecretId(secretId);
        return getClient()
            .then(client => client.getSecret(secretInfo.keyVaultUrl, secretInfo.secretName, ''))
            .catch(err => {
                return Promise.reject({
                    message: `Error in querying keyvault with id '${secretId}': ${err.message}.`
                });
            })
            .then(secret => secret.value);
    }

    function setSecret(secretId, value) {
        var secretInfo = parseSecretId(secretId);
        return getClient()
            .then(client => client.setSecret(secretInfo.keyVaultUrl, secretInfo.secretName, value))
            .then(result => 'OK');
    }

    function deleteSecret(secretId) {
        var secretInfo = parseSecretId(secretId);
        return getClient().then(client => client.deleteSecret(secretInfo.keyVaultUrl, secretInfo.secretName));
    }

    return {
        getSecretOrThrow: getSecret,
        getSecret: s => getSecret(s).catch(err => null),
        setSecret: setSecret,
        deleteSecret: deleteSecret
    };
};
