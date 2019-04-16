// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import { nodeServiceGetApi } from 'datax-common';
import { ApiNames } from './apiConstants';

export const functionEnabled = () => nodeServiceGetApi(ApiNames.FunctionEnabled);
