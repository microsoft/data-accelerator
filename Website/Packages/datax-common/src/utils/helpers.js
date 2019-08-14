
// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
export function isValidNumberAboveZero(value) {
    const number = Number(value);
    const isNumber = !isNaN(number);
    const isValid = isNumber && number > 0 && value[0] !== '0';
    return isValid;
}