// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import * as d3 from 'd3';

var gauge = function(container, configuration) {
    var that = {};
    var config = {
        size: 710,
        clipWidth: 200,
        clipHeight: 110,
        ringInset: 20,
        ringWidth: 20,

        pointerWidth: 10,
        pointerTailLength: 5,
        pointerHeadLengthPercent: 0.9,

        minValue: 0,
        maxValue: 10,

        minAngle: -90,
        maxAngle: 90,

        transitionMs: 750,

        majorTicks: 5,
        labelFormat: d3.format('d'),
        labelInset: 10,

        arcColorFn:
            configuration.colorScale ||
            d3
                .scaleLinear()
                .domain([0, 0.22, 1])
                .range(['green', '#eff43e', 'red'])
                .interpolate(d3.interpolateRgb),

        format: v => {
            if (v === undefined || v == null) {
                return 'unknown';
            } else if (isNaN(v)) {
                return '1+ hours';
            } else return Math.floor(v) + ' min';
        }
    };

    var range = undefined;
    var r = undefined;
    var pointerHeadLength = undefined;
    var value = 0;

    var svg = undefined;
    var arc = undefined;
    var scale = undefined;
    var ticks = undefined;
    var tickData = undefined;
    var pointer = undefined;
    var valueText = undefined;

    var donut = d3.pie();

    function deg2rad(deg) {
        return (deg * Math.PI) / 180;
    }

    function newAngle(d) {
        var ratio = scale(d);
        var newAngle = config.minAngle + ratio * range;
        return newAngle;
    }

    function configure(configuration) {
        var prop = undefined;
        for (prop in configuration) {
            config[prop] = configuration[prop];
        }

        range = config.maxAngle - config.minAngle;
        r = config.size / 2;
        pointerHeadLength = Math.round(r * config.pointerHeadLengthPercent);

        // a linear scale that maps domain values to a percent from 0..1
        scale = d3
            .scaleLinear()
            .range([0, 1])
            .domain([config.minValue, config.maxValue]);

        ticks = scale.ticks(config.majorTicks);
        tickData = d3.range(config.majorTicks).map(function() {
            return 1 / config.majorTicks;
        });

        arc = d3
            .arc()
            .innerRadius(r - config.ringWidth - config.ringInset)
            .outerRadius(r - config.ringInset)
            .startAngle(function(d, i) {
                var ratio = d * i;
                return deg2rad(config.minAngle + ratio * range);
            })
            .endAngle(function(d, i) {
                var ratio = d * (i + 1);
                return deg2rad(config.minAngle + ratio * range);
            });
    }
    that.configure = configure;

    function centerTranslation() {
        return 'translate(' + r + ',' + r + ')';
    }

    function isRendered() {
        return svg !== undefined;
    }
    that.isRendered = isRendered;

    function render(newValue) {
        svg = d3
            .select(container)
            .append('svg:svg')
            .attr('class', 'gauge')
            .attr('width', config.clipWidth)
            .attr('height', config.clipHeight);

        var centerTx = centerTranslation();

        var arcs = svg
            .append('g')
            .attr('class', 'arc')
            .style('fill', 'steelblue')
            .attr('transform', centerTx);

        arcs.selectAll('path')
            .data(tickData)
            .enter()
            .append('path')
            .attr('fill', function(d, i) {
                return config.arcColorFn(d * i);
            })
            .attr('d', arc);

        var lg = svg
            .append('g')
            .attr('class', 'label')
            .style('text-anchor', 'middle')
            .style('font-size', '12px')
            .style('font-weight', 'bold')
            .style('fill', '#666')
            .attr('transform', centerTx);
        lg.selectAll('text')
            .data(ticks)
            .enter()
            .append('text')
            .attr('transform', function(d) {
                var ratio = scale(d);
                var newAngle = config.minAngle + ratio * range;
                return 'rotate(' + newAngle + ') translate(0,' + (config.labelInset - r) + ')';
            })
            .text(config.labelFormat);

        var lineData = [
            [config.pointerWidth / 2, 0],
            [0, -pointerHeadLength],
            [-(config.pointerWidth / 2), 0],
            [0, config.pointerTailLength],
            [config.pointerWidth / 2, 0]
        ];

        var pointerLine = d3.line().curve(d3.curveLinear);
        var pg = svg
            .append('g')
            .data([lineData])
            .attr('class', 'pointer')
            //.style('fill', '#e85116')
            .style('fill', 'black')
            //.style('stroke', '#b64011')
            .attr('transform', centerTx);

        valueText = svg
            .append('g')
            .attr('class', 'label')
            .style('text-anchor', 'middle')
            .style('font-size', '12px')
            .style('font-weight', 'bold')
            //.style('fill', '#666')
            .append('text')
            .attr('x', r)
            .attr('y', r + 20);

        pointer = pg
            .append('path')
            .attr('d', pointerLine /*function(d) { return pointerLine(d) +'Z';}*/)
            .attr('transform', 'rotate(' + config.minAngle + ')');

        update(newValue);

        return that;
    }
    that.render = render;
    function update(newValue, newConfiguration) {
        if (newConfiguration !== undefined) {
            configure(newConfiguration);
        }

        if (!isNaN(newValue)) {
            var ratio = newValue > config.maxValue ? 1 : scale(newValue);
            var newAngle = config.minAngle + ratio * range;
            pointer
                .transition()
                .duration(config.transitionMs)
                .ease(d3.easeElastic)
                .attr('transform', 'rotate(' + newAngle + ')');
        }

        valueText.text(config.format(newValue));
        return that;
    }

    that.update = update;

    configure(configuration);

    return that;
};

export default gauge;
