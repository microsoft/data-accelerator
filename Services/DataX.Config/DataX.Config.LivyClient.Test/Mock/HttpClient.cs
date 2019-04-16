// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace DataX.Config.LivyClient.Test.Mock
{
    public class HttpClient : ILivyHttpClient
    {
        private readonly string _userName;
        private readonly string _password;
        private readonly Dictionary<string, LivyHttpResult> _responses;

        public HttpClient(string userName, string password, Dictionary<string, LivyHttpResult> responses)
        {
            this._userName = userName;
            this._password = password;
            this._responses = responses;
        }

        public Task<LivyHttpResult> ExecuteHttpRequest(HttpMethod method, Uri uri, string body = "")
        {
            return Task.FromResult(_responses[ComposeKey(method, uri)]);
        }

        public static string ComposeKey(HttpMethod method, Uri uri)
        {
            return $"{method}- {uri}"; 
        }
    }
}
