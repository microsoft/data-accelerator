// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
const winston = require('winston');
const config = require('./config');
require('winston-daily-rotate-file');

var pid = process.pid;

const fs = require('fs');
var logsDir = 'logs/';

if (!fs.existsSync(logsDir)) {
    fs.mkdirSync(logsDir);
}

var rotatingFileTransport = new winston.transports.DailyRotateFile({
    name: 'mainlog',
    handleExceptions: true,
    filename: logsDir,
    datePattern: logsDir + 'yyyy-MM-dd.' + pid + '.',
    prepend: true,
    maxsize: 200 << 20,
    maxFiles: 10,
    json: false,
    level: config.env.name === 'dev' ? 'debug' : 'info'
});

var consoleTransport = new winston.transports.Console({
    json: false,
    timestamp: true,
    humanReadableUnhandledException: true,
    colorize: true
});

var infoFileTransport = new winston.transports.File({ filename: __dirname + '/logs/debug.log', json: false });
var exceptionFileTransport = new winston.transports.File({ filename: __dirname + '/logs/exception.log', json: false });

var logger = new winston.Logger({
    transports: config.env.name === 'dev' ? [rotatingFileTransport, consoleTransport] : [rotatingFileTransport],
    exceptionHandlers: [consoleTransport, exceptionFileTransport],
    exitOnError: true
});

module.exports = logger;
