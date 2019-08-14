// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Contract.Exception;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.DatabricksClient
{
    public class DatabricksHttpClient : IDatabricksHttpClient
    {
        private HttpClient _client;

        public DatabricksHttpClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<DatabricksHttpResult> ExecuteHttpRequest(HttpMethod method, Uri uri, string body = "")
        {
            try
            {
                var response = await ExecuteHttpRequestInternal(method, uri, body);
                var message = await response.Content.ReadAsStringAsync();
                return new DatabricksHttpResult()
                {
                    IsSuccess = response.IsSuccessStatusCode,
                    StatusCode = response.StatusCode,
                    Content = message
                };
            }
            catch (Exception ex)
            {
                throw new GeneralException($"failed in calling '{uri}' due to error:{ex.Message}");
            }
        }

        public Task<HttpResponseMessage> ExecuteHttpRequestInternal(HttpMethod method, Uri uri, string body = "")
        {
            switch (method)
            {
                case HttpMethod.Get:
                    return _client.GetAsync(uri);
                case HttpMethod.Post:
                    return _client.PostAsync(uri, new StringContent(body, Encoding.UTF8, "application/json"));
                case HttpMethod.Delete:
                    return _client.DeleteAsync(uri);
                default:
                    throw new GeneralException($"Unsupported method :'{method}'");
            }
        }
    }
}
