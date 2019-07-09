// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import { serviceGetApi, Constants } from 'datax-common';

export const getFlowsList = () =>
    serviceGetApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.flow, 'flow/getall/min');
