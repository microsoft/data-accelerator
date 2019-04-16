// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
export function getApiErrorMessage(error) {
    if (error && error.message) {
        return error.message;
    } else if (error) {
        return error;
    } else {
        return 'Error occured but service did not return information about the error';
    }
}
