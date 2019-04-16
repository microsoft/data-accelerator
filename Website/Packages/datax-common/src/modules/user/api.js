// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import { nodeServiceGetApi } from '../../utils';

export function getUser() {
    return nodeServiceGetApi('user');
}
