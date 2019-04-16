// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
function format(msg) {
    return new Date().toISOString() + '-' + msg;
}

module.exports = {
    info: msg => {
        console.info(format(msg));
    },
    error: msg => {
        console.error(format(msg));
    }
};
