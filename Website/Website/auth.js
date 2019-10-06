// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
const webComposition = require('./web.composition.json');
const passport = require('passport');
const OIDCStrategy = require('passport-azure-ad').OIDCStrategy;
const AuthenticationContext = require('adal-node').AuthenticationContext;
const rp = require('request-promise');
const csrf = require('csurf');

const graphId = 'https://graph.windows.net';
const azureUrl = 'https://management.azure.com';

function resourceIdToPropName(resourceId) {
    return Buffer.from(resourceId).toString('base64');
}

function getServiceToken(user, resourceId) {
    const propName = resourceIdToPropName(resourceId);
    if (user.serviceTokens) {
        return user.serviceTokens[propName];
    }
}

function getRefreshToken(user, resourceId) {
    const existingToken = getServiceToken(user, resourceId);
    if (existingToken) {
        return existingToken.refreshToken;
    } else {
        return user.appRefreshToken;
    }
}

function saveServiceToken(user, resourceId, token) {
    const propName = resourceIdToPropName(resourceId);
    if (!user.serviceTokens) {
        user.serviceTokens = {};
    }

    user.serviceTokens[propName] = token;
}

function preparePassport(conf) {
    const tenantName = conf.tenantName;
    const identityMetadata = `https://login.microsoftonline.com/${tenantName}/v2.0/.well-known/openid-configuration`;
    let authenticatedUserTokens = [];

    passport.serializeUser(function(user, done) {
        done(null, user);
    });
    passport.deserializeUser(function(user, done) {
        done(null, user);
    });

    passport.use(
        new OIDCStrategy(
            {
                identityMetadata: identityMetadata,
                clientID: conf.clientId,
                responseType: 'code',
                responseMode: 'form_post',
                redirectUrl: conf.authReturnUrl,
                passReqToCallback: true,
                allowHttpForRedirectUrl: true,
                clientSecret: conf.clientSecret,
                validateIssuer: true,
                scope: ['email', 'profile', 'offline_access']
            },
            function verify(req, iss, sub, profile, jwtClaims, accessToken, refreshToken, params, done) {
                if (!profile.oid) {
                    return done(new Error('No valid user auth ID'), null);
                }

                profile.appRefreshToken = refreshToken;
                profile.email = profile.email || jwtClaims.email;
                done(null, profile);
            }
        )
    );

    return passport;
}

function initialize(host) {
    const env = host.conf.env;
    const tenantName = env.tenantName;
    const subscriptionId = env.subscriptionId;
    const authContext = new AuthenticationContext(`https://login.microsoftonline.com/${tenantName}`);
    const serviceClusterUrl = env.serviceClusterUrl;

    function resourceToResourceId(resource) {
        if (resource === 'service') {
            return env.serviceResourceId;
        } else if (resource === 'graph') {
            return graphId;
        } else if (resource === 'azure') {
            return azureUrl;
        } else {
            throw new Error(`unknown resource:'${resource}'`);
        }
    }

    function retrieveToken(refreshToken, resourceId, callback) {
        authContext.acquireTokenWithRefreshToken(refreshToken, env.clientId, env.clientSecret, resourceId, callback);
    }

    // On first sign-in this might fail due to consent not being cached in AAD
    // Retrying usually fixes things.
    function robustRetrieveToken(refreshToken, resourceId, callback, retryCount = 0) {
        retrieveToken(refreshToken, resourceId, (err, result) => {
            if (err) {
                if (retryCount <= 6) {
                    setTimeout(() => {
                        robustRetrieveToken(refreshToken, resourceId, callback, ++retryCount);
                    }, Math.pow(2, retryCount) * 1000);
                } else {
                    callback(err);
                }
            } else {
                callback(null, result);
            }
        });
    }

    const updateTokenIfNeeded = (user, resourceId) =>
        new Promise((resolve, reject) => {
            const serviceToken = getServiceToken(user, resourceId);
            if (!serviceToken || new Date(serviceToken.expiresOn) < new Date()) {
                let refreshToken = getRefreshToken(user, resourceId);
                robustRetrieveToken(refreshToken, resourceId, (error, result) => {
                    if (error) {
                        return reject(error);
                    }

                    saveServiceToken(user, resourceId, result);
                    return resolve(result.accessToken);
                });
            } else {
                resolve(serviceToken.accessToken);
            }
        });

    /**
     * Options is passed directly to the request module. Resource is 'test', 'production', or a custom resource id
     */
    const makeRequest = (req, resource, url, options) =>
        updateTokenIfNeeded(req.user, resourceToResourceId(resource)).then(accessToken =>
            rp(
                Object.assign(
                    {
                        auth: {
                            bearer: accessToken
                        },
                        agentOptions: {
                            rejectUnauthorized: env.rejectUnauthorized
                        },
                        json: true,
                        uri: url,
                        resolveWithFullResponse: true
                    },
                    options
                )
            )
        );

    // Requests in oneBox mode don't need auth
    const makeRequestLocal = (req, resource, url, options) =>
        rp(
            Object.assign(
                {
                    json: true,
                    uri: url,
                    resolveWithFullResponse: true
                },
                options
            )
        );

    const defaultParams = {
        subscriptionId: subscriptionId
    };

    const defaultConfig = {
        subscription: subscriptionId
    };

    /**
     * call services on service fabric or services hosted on Kubernetes
     * @param {http request coming from client} req
     * @param {query object to call Service Fabric service} query
     * Example:
     * {
     *   application: "DataX.Flow",// This does not apply the kubernetes scenario. This applies only to the ServiceFabric scenario.
     *   the rest of the parameters stay the same for the Kubernetes scenario as well.
     *   service: "Flow.ManagementService",
     *   method: "GET", // or POST
     *   headers: {"Content-type": "application/json"} // optional headers
     *   body: "" // optional POST body
     *   api: "blah",
     *   params: {
     *       num1: 1,
     *       num2: 2
     *   }
     * }
     */
    function queryHub(req, query) {
        let url;
        if (env.localServices[query.service]) {
            url = `${env.localServices[query.service]}/api/${query.api}`;
        } else if (env.kubernetesServices && env.kubernetesServices[query.service]) {
            url = `${env.kubernetesServices[query.service]}/api/${query.api}`;
        } else {
            url = `${serviceClusterUrl}/api/${query.application}/${query.service}/${query.api}`;
        }

        let options = {
            method: query.method || 'GET',
            qs: query.params,
            headers: query.headers || {},
            body: query.body
        };

        if (env.enableLocalOneBox) {
            return makeRequestLocal(req, 'service', url, options);
        } else {
            return makeRequest(req, 'service', url, options);
        }
    }

    /**
     * call services on service fabric
     * @param {http request coming from client} req
     * @param {query object to call Service Fabric service} query
     */
    function queryService(req, query) {
        // the Services expects different paramenter name of subscription id
        // on POST, it expects 'subscription', on GET, it expects 'subscriptionId'
        // we might want to unify them in future refactoring.
        if (query.method == 'POST' && query.body) {
            Object.assign(query.body, defaultConfig);
        } else if (query.method == 'GET' && query.params) {
            Object.assign(query.params, defaultParams);
        }

        return queryHub(req, query);
    }

    const makeGetRequest = (req, resource, url, options) => makeRequest(req, resource, url, Object.assign({}, options, { method: 'GET' }));

    function getProfilePhoto(req) {
        if (!req.user) {
            return new Error('User not signed in');
        }
        let url = `https://graph.windows.net/${tenantName}/users/${req.user.oid}/thumbnailPhoto?api-version=1.6`;
        let options = {
            encoding: null,
            headers: {
                Accept: 'image/*'
            }
        };

        return makeGetRequest(req, 'graph', url, options);
    }

    return {
        preparePassport: () => preparePassport(env),
        queryService: queryService,
        getProfilePhoto: getProfilePhoto
    };
}

function ensureAuthenticated(req, res, next) {
    if (req.isAuthenticated()) {
        return next();
    } else if (req.xhr) {
        res.status(401).end();
    } else {
        if (req.session) {
            req.session.returnTo = req.originalUrl;
        }
        res.redirect('/login');
    }
}

const logoutUrl = 'https://login.windows.net/common/oauth2/logout';

// this function assume that you already did app.use(bodyParser.urlencoded({ extended : true }));
exports.initialize = function(host) {
    const telemetryClient = host.telemetryClient;
    const app = host.app;
    const query = initialize(host);
    host.query = query;

    // Send mock role info in OneBox mode
    if (host.conf.env.enableLocalOneBox) {
        app.get('/api/functionenabled', function(req, res) {
            res.type('application/json').send(functionEnabled(Object.keys(webComposition.api), host.conf.env.enableLocalOneBox));
        });
    } else {
        // Not OneBox mode
        query.preparePassport();
        app.use(passport.initialize());
        app.use(passport.session());

        const loginOptions = {
            failWithError: true
        };
        app.get('/login', passport.authenticate('azuread-openidconnect', loginOptions));
        app.post('/authReturn', passport.authenticate('azuread-openidconnect', loginOptions), function(req, res) {
            if (req.session) {
                res.redirect(req.session.returnTo || '/');
                delete req.session.returnTo;
            } else {
                res.redirect('/');
            }
        });

        app.all('*', ensureAuthenticated);
        app.get('/logout', function(req, res) {
            req.logout();
            res.redirect(logoutUrl);
        });
        app.get('/api/user/photo', (req, res) =>
            query
                .getProfilePhoto(req)
                .then(response => res.type(response.headers['content-type']).send(response.body))
                .catch(error => {
                    console.error(error);
                    res.status(500).json({ error: error.message });
                })
        );

        // https://www.npmjs.com/package/csurf
        // Node.js CSRF (Cross Site Request Forgery) protection middleware to add protection to POST API calls.
        // Start using CSRF protection for all POST APIs declared after this initialization.
        // You should never declare any POST handlers above this execution statement besides
        // the login handler or else they will not receive the CSRF protection.
        app.use(csrf());

        // Get user info and pass CSRF token back to client
        app.get('/api/user', function(req, res) {
            res.cookie('csrfToken', req.csrfToken());
            res.type('application/json').send({ id: req.user.email, name: req.user.displayName });
        });

        app.get('/api/functionenabled', function(req, res) {
            res.type('application/json').send(functionEnabled(req.user._json.roles, host.conf.env.enableLocalOneBox));
        });

        app.get('/api/isdatabrickssparktype', function(req, res) {
            res.type('application/json').send(isDatabricksSparkType(process.env.DATAX_SPARK_TYPE));
        });

        app.all('/api/*', (req, res, next) => {
            let roles = req.user._json.roles;
            let errorResult = checkPermission(roles, webComposition.api, req);
            if (!errorResult) {
                return next();
            } else {
                telemetryClient.trackNodeHttpRequest({ request: req, response: errorResult });
                return res.json({ body: errorResult, error: errorResult });
            }
        });

        // CSRF protection error handler
        app.use((err, req, res, next) => {
            if (err.code !== 'EBADCSRFTOKEN') {
                return next(err);
            }

            // Handle CSRF token errors here
            const message = 'The server understood the request but refuses to authorize it.';
            res.status(403).json({ body: message, error: message });
        });
    }

    app.post('/api/apiservice', (req, res) => serviceResponder(req, res, query.queryService));
};

function checkPermission(roles, permissionRulesApi, req) {
    let apiCall = req.url == '/api/apiservice' ? req.body.application + '/' + req.body.service + '/' + req.body.api : req.url;
    if (
        apiCall == '/api/web-composition' ||
        apiCall == '/api/enableLocalOneBox' ||
        (roles &&
            roles.some(r =>
                permissionRulesApi[r] !== undefined ? permissionRulesApi[r].some(s => new RegExp(s, 'i').test(apiCall)) : false
            ))
    ) {
        return;
    } else {
        return "Oops! Sorry you don't have permission. Please contact Admin for permissions";
    }
}

const serviceResponder = (req, res, responder) =>
    responder(req, req.body)
        .then(response =>
            res.json({
                body: response.body,
                status: response.statusCode,
                headers: response.headers
            })
        )
        .catch(error => {
            console.error(error);
            req.logout();
            res.status(500).json({ error: error.message });
        });

function functionEnabled(roles, enableLocalOneBox) {
    let supportedFunctionalities = {};
    let isWriter =
        roles &&
        roles.some(r =>
            webComposition.api[r] !== undefined ? webComposition.api[r].some(s => new RegExp(s, 'i').test('WriterRole')) : false
        )
            ? true
            : false;

    // Enable/disable features based on user role and local vs cloud mode.
    // Some of the features disabled in local mode is because they are not implemented yet.
    webComposition.functionsEnabled.enabledForWriter.forEach(function(functionalityName) {
        supportedFunctionalities[functionalityName] = enableLocalOneBox
            ? webComposition.functionsEnabled.disabledForLocalOneBox.includes(functionalityName)
                ? isWriter && !enableLocalOneBox
                : isWriter
            : isWriter;
    });

    return supportedFunctionalities;
}

function isDatabricksSparkType(sparkType) {
    return sparkType && sparkType.toLowerCase() == 'databricks' ? true : false;
}
