// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.DatabricksClient
{
    public interface IDatabricksHttpClient
    {
        Task<DatabricksHttpResult> ExecuteHttpRequest(HttpMethod method, Uri uri, string body = "");
    }
}
