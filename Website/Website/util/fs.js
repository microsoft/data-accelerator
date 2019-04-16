// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
const fs = require('fs');
const path = require('path');
const moment = require('moment');
const common = require('./common');

module.exports = {
    ensureFolderExists: function(folder) {
        if (!fs.existsSync(folder)) {
            fs.mkdirSync(folder);
        }
    },

    appendTimestampToFileName: function(fileName) {
        let timestamp = moment().format('YYYYMMDDHHmmss');
        if (fileName) {
            let extName = path.extname(fileName);
            fileName = path.basename(fileName, extName) + '_' + timestamp + extName;
        } else {
            fileName = timestamp;
        }

        return fileName;
    },

    trailingSlash: function(path) {
        if (!path) return common.error(`path cannot be null/empty!`);
        else if (path[path.length - 1] != '/') return path + '/';
        else return path;
    },

    removeHeadingSlashes: function(path) {
        if (!path) return common.error(`path cannot be null/empty!`);
        var i = 0;
        for (; i < path.length; i++) if (path[i] != '/') break;
        return path.substring(i);
    }
};
