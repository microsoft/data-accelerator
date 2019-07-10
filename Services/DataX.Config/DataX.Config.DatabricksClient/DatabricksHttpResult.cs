// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace DataX.Config.DatabricksClient
{
    public class DatabricksHttpResult
    {
        public HttpStatusCode StatusCode;
        public string Content;
        public bool IsSuccess;
    }
}
