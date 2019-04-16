// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import { serviceGetApi } from 'datax-common';
import { Constants } from '../../common/apiConstants';

export const getFlowsList = () =>
    serviceGetApi(Constants.serviceRouteApi, Constants.serviceApplication, Constants.services.flow, 'flow/getall/min');
