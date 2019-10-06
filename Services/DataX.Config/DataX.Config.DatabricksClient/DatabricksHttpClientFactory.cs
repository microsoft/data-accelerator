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

namespace DataX.Config.DatabricksClient
{
    [Shared]
    [Export(typeof(IDatabricksHttpClientFactory))]
    public class DatabricksHttpClientFactory : IDatabricksHttpClientFactory
    {
        public IDatabricksHttpClient CreateClientWithBearerToken(string dbToken)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", dbToken);
            return new DatabricksHttpClient(client);
        }
    }
}
