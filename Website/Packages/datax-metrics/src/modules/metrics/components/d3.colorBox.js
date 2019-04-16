// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import * as d3 from 'd3';

var colorBox = function(container, configuration) {
    var that = {};
    var config = {
        transitionMs: 750
    };
    var div = undefined;
    var scale = undefined;

    function configure(configuration) {
        var prop = undefined;
        for (prop in configuration) {
            config[prop] = configuration[prop];
        }

        scale = d3
            .scaleLinear()
            .range([0, 1])
            .domain([config.minValue, config.maxValue]);
    }
    that.configure = configure;

    function centerTranslation() {
        return 'translate(' + r + ',' + r + ')';
    }

    function isRendered() {
        return div !== undefined;
    }
    that.isRendered = isRendered;

    function render(newValue) {
        div = d3.select(container);
        update(newValue);
        return that;
    }
    that.render = render;

    function normalizeValue(value) {
        if (isNaN(value)) {
            return NaN;
        } else if (value > config.maxValue) {
            return 1;
        } else if (value < config.minValue) {
            return 0;
        } else {
            return scale(value);
        }
    }

    function update(newValue, newConfiguration) {
        if (newConfiguration !== undefined) {
            configure(newConfiguration);
        }

        var ratio = normalizeValue(newValue);
        div.transition()
            .duration(config.transitionMs)
            .ease(d3.easeElastic)
            .style('background-color', config.colorScale(ratio));
        return that;
    }

    that.update = update;

    configure(configuration);

    return that;
};

export default colorBox;
