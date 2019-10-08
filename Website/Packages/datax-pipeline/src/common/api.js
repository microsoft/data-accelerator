// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import { nodeServiceGetApi, ApiNames } from 'datax-common';

export const functionEnabled = () => nodeServiceGetApi(ApiNames.FunctionEnabled);
export const isDatabricksSparkType = () => nodeServiceGetApi(ApiNames.IsDatabricksSparkType);
