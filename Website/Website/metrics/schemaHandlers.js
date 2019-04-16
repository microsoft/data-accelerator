// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
var handlers = [];

module.exports = {
    forSchema: function(schema) {
        return handlers.find(function(handler) {
            return handler.canHandle(schema);
        });
    },

    register: function(handler) {
        handlers.push(handler);
    }
};
