// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

export function getDefaultQuery(enableLocalOneBox) {
    if (enableLocalOneBox) {
        return defaultQueryLocal;
    } else {
        return defaultQuery;
    }
}
//User can use the sample or tutorial or intellisense to have starting query. Below allows default to have 5 blank lines.
export const defaultQuery = `

`;

export const defaultQueryLocal = `--DataXQuery--
events = SELECT MAX(temperature) as maxTemp
        FROM
        DataXProcessedInput;

maxTemperature = CreateMetric(events, maxTemp);

OUTPUT maxTemperature TO Metrics;
`;