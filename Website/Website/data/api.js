// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import { nodeServiceGetApi } from 'datax-common';

export function getWebComposition() {
    return nodeServiceGetApi('web-composition');
}
