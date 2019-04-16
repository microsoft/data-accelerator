// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

const logger = require('./util/consolelogger');
logger.info('Initializing server...');

const host = require('./host');
const bodyParser = require('body-parser');
const session = require('express-session');
const helmet = require('helmet');

async function run(host) {
    const app = host.app;
    const server = host.server;
    const config = require('./config');
    host.conf = config;
    const enableLocalOneBox = host.conf.env.enableLocalOneBox;

    // https://www.npmjs.com/package/helmet
    // Use helmet to secure express apps by setting various HTTP headers for security protection
    // such as enabling HSTS (HTTP Strict Transport Security), common XSS (cross site scripting)
    // protection, and more.
    app.use(helmet());

    if (!enableLocalOneBox) {
        await require('./securedSettings')(host);
        logger.info(`initialized secured settings`);
    } else {
        const uuidv4 = require('uuid/v4');
        // Set any secured settings that is needed for OneBox mode here directly (these are retrieved from keyvault in default cloud mode)
        config.env.sessionSecret = uuidv4();
    }

    //TODO: move service initialization to the host
    const telemetryClient = require('./appinsights')(host);
    logger.info(`initialized application insights client`);

    const auth = require('./auth');
    const dist = require('./util/dist');

    // get websition composition config
    const webComposition = require('./web.composition.json');
    logger.info(`initialized website composition config`);

    app.all('*', (req, res, next) => {
        telemetryClient.trackNodeHttpRequest({ request: req, response: res });
        return next();
    });

    app.use(bodyParser.urlencoded({ extended: true }));
    app.use(bodyParser.json());

    // https://github.com/expressjs/session#cookie-options
    // When using secure cookies over HTTPS, we need to configure to trust the first proxy which in our case is Azure.
    // When using HTTP for local development, we don't need to set any proxy and disable secured cookies as secured
    // cookies are only set when website is accessed via HTTPS.
    if (!config.env.isLocal) {
        app.set('trust proxy', 1);
    }

    app.use(
        session({
            secret: config.env.sessionSecret,
            resave: false,
            saveUninitialized: true,
            cookie: {
                secure: config.env.isLocal ? false : true, // Whether to use secured cookies
                httpOnly: true
            }
        })
    );

    logger.info(`initialized session store`);

    auth.initialize(host);
    logger.info(`initialized authentication handlers`);

    dist.initialize(app);
    logger.info(`initialized dist handlers`);

    const metricService = require('./metrics/metricService');
    metricService.init(app, server);
    logger.info(`initialized metrics service`);

    const sendMainPage = (req, res, next) => {
        return dist.sendStaticFile(res, 'index.html');
    };

    app.get('/', sendMainPage);

    // Register routh paths supported by our website
    webComposition.client.pages
        .filter(page => page.enable)
        .forEach(page => {
            if (!page.externalUrl) {
                const routePath = page.supportSubRoute ? `${page.routePath}*` : page.routePath;
                app.get(routePath, sendMainPage);
                logger.info(`initialized route handler for ${routePath}`);
            }
        });

    // API for returning website composition config to client
    app.get('/api/web-composition', (request, response) => {
        const clientComposition = webComposition.client;
        response.json(clientComposition);
    });

    // API for passing enableLocalOneBox setting to client
    app.get('/api/enableLocalOneBox', (request, response) => {
        response.json({
            enableLocalOneBox: enableLocalOneBox
        });
    });

    var port = config.env.port;

    server.listen(port, function() {
        telemetryClient.trackEvent({ name: 'datax/web/server/start' });
        logger.info(`The server is running at http://localhost:${port}/`);
    });
}

run(host).catch(err => {
    logger.error(err.message);
    process.exit(500);
});
