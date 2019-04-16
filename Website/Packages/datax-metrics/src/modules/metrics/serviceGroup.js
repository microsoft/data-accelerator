// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
class ServiceGroup {
    constructor(services) {
        this._isStarted = false;
        this.services = [];
        if (services) services.forEach(this.add.bind(this));
    }

    add(service) {
        if (service) this.services.push(service);
        return this;
    }

    start() {
        if (this._isStarted) return;
        this.services.forEach(s => {
            s.start();
        });
        this._isStarted = true;
    }

    stop() {
        if (!this._isStarted) return;
        this.services.forEach(s => {
            s.stop();
        });
        this._isStarted = false;
    }

    isStarted() {
        return this._isStarted;
    }
}

module.exports = ServiceGroup;
