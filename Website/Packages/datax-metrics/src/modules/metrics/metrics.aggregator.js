// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
const sum = data => data.reduce((s, d) => (isNaN(d) ? s : isNaN(s) ? d : s + d), NaN);

module.exports = {
    sum: sum,
    max: data => data.reduce((s, d) => (isNaN(d) ? s : isNaN(s) ? d : s > d ? s : d), NaN),
    count: data => data.length,
    average: data => {
        var valid = data.filter(d => !isNaN(d));
        return valid.length == 0 ? NaN : sum(valid) / valid.length;
    },
    first: data => (data.length > 0 ? data[0] : NaN)
};
