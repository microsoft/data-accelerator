// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Composition;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace DataX.Config.LivyClient
{
    [Shared]
    [Export(typeof(ILivyHttpClientFactory))]
    public class LivyHttpClientFactory : ILivyHttpClientFactory
    {
        public ILivyHttpClient CreateClientWithBasicAuth(string userName, string password)
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("X-Requested-By", "user");

            var byteArray = Encoding.ASCII.GetBytes($"{userName}:{password}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            return new LivyHttpClient(client);
        }
    }
}
