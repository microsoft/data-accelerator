// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
var cacheSize = 10000;
var dataCache = [];

function initialize(app, server) {
    app.post('/api/data/upload', function(req, res) {
        console.log('receving data from ' + req.ip);
        cacheLocalData(req.body);

        res.end('done');
    });
}

function cacheLocalData(data) {
    if (data.propertyIsEnumerable) {
        data.forEach(item => {
            // Set the metric name by prepending the product or app name based on if its default metric or custom metric.
            // This is because the metric schema for default metric and custom metric is different
            if (item.Product) {
                item.MetricName = item.Product + ':' + item.MetricName; //custom metric
            } else {
                item.MetricName = item.app + ':' + item.met; //default metric
            }
            dataCache.push(item);
            // Set the event timestamp to current
            item.uts = new Date().getTime();
        });

        const overhead = dataCache.length - cacheSize;
        if (overhead > 0) {
            dataCache.splice(0, overhead);
        }
    }
}

function getLocalData(name, startTime, endTime) {
    let metricsData = [];

    // Get raw data back for the timerange expected
    dataCache.forEach(item => {
        // default metrics case
        if (item.met != undefined) {
            if (item.MetricName.toLowerCase() == name.toLowerCase()) {
                const data = {
                    uts: item.uts,
                    val: item.val
                };
                //check if data is between requested time
                if (data.uts >= startTime && data.uts <= endTime) {
                    metricsData.push(JSON.stringify(data));
                }
            }
        } //one pivot only
        else if (item.MetricName.toLowerCase() == name.toLowerCase()) {
            // custom metrics case
            const data = {
                uts: item.uts,
                val: item.Metric,
                pivot1: item.Pivot1
            };
            //check if data is between requested time
            if (data.uts >= startTime && data.uts <= endTime) {
                metricsData.push(JSON.stringify(data));
            }
        }
    });
    return metricsData;
}

module.exports = {
    init: initialize,
    getLocalData
};
