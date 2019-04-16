// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace DataX.Config.LivyClient
{
    public interface ILivyHttpClient
    {
        Task<LivyHttpResult> ExecuteHttpRequest(HttpMethod method, Uri uri, string body = "");
    }
}
