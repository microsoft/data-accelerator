// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;

namespace DataX.Config.LivyClient.Test.Mock
{
    public class HttpClientFactory : ILivyHttpClientFactory
    {
        public Dictionary<string, LivyHttpResult> _responses = new Dictionary<string, LivyHttpResult>();

        public HttpClientFactory AddResponse(HttpMethod method, string uri, string content, HttpStatusCode code = HttpStatusCode.OK)
        {
            var key = HttpClient.ComposeKey(method, new Uri(uri));
            var response = new LivyHttpResult
            {
                IsSuccess = code == HttpStatusCode.OK,
                StatusCode = code,
                Content = content
            };

            _responses.Add(key, response);
            return this;
        }

        public ILivyHttpClient CreateClientWithBasicAuth(string userName, string password)
        {
            return new HttpClient(userName, password, _responses);
        }
    }
}
