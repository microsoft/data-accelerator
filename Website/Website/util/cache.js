// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
const Q = require('q');

/**
 * factory method of generating a cache in memory
 */
function factory() {
    var cache = {};

    /**
     * internal method to update value of a storage item in the cache.
     * @param {the storage item in cache} item
     * @param {value to put into the storage item} value
     * @param {expiry of the value} timeoutInMilliseconds
     */
    function put(item, value, timeoutInMilliseconds) {
        item.inProgress = false;
        item.val = value;
        item.expireAt = Date.now() + timeoutInMilliseconds;

        item.deferreds.forEach(deferred => {
            deferred.resolve(value);
        });

        item.deferreds = undefined;

        return value;
    }

    function catchError(item, error) {
        item.inProgress = false;
        item.val = undefined;
        item.expireAt = undefined;

        item.deferreds.forEach(deferred => {
            deferred.reject(error);
        });

        item.deferreds = undefined;

        return error;
    }

    return {
        /**
         * get a value from the cache with the name or generate it with the getter.
         * @param {name of the cached value} name
         * @param {generator of value if the value is not in the cache or the value is expired} getter
         * @param {expiry in milliseconds to for generated value} timeoutInMilliseconds
         */
        get: function(name, getter, timeoutInMilliseconds) {
            let item = cache[name];
            if (item && item.inProgress) {
                var deferred = Q.defer();
                item.deferreds.push(deferred);
                return deferred.promise;
            }

            if (!item || !item.expireAt || item.expireAt < Date.now()) {
                if (getter) {
                    var deferred = Q.defer();
                    item = {
                        inProgress: true,
                        deferreds: [deferred]
                    };

                    cache[name] = item;

                    getter()
                        .then(value => put(item, value, timeoutInMilliseconds))
                        .catch(err => catchError(item, err));

                    return deferred.promise;
                } else {
                    if (item) delete cache[name];
                    return Q.resolve(undefined);
                }
            } else {
                return Q.resolve(item.val);
            }
        }
    };
}

module.exports = factory;
