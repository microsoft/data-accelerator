// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Contract;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Utility.ServiceCommunication
{
    /// <summary>
    /// Concrete implementation of inter-service communication client.
    /// </summary>
    public class InterServiceCommunicator : IDisposable
    {
        private const string _ReverseProxyPort = "19081";
        private readonly HttpClient _httpClient;

        public InterServiceCommunicator(TimeSpan timeout)
        {
            var handler = new WinHttpHandler
            {
                ServerCertificateValidationCallback = (message, cert, chain, errors) => { return true; },
                // We need to set timeout for handler first
                // Setting timeout for httpClient alone is not good enough
                SendTimeout = timeout,
                ReceiveDataTimeout = timeout,
                ReceiveHeadersTimeout = timeout
            };

            _httpClient = new HttpClient(handler)
            {
                // Default http timeout is 100s, increase it to 4 min since few key mainline scenarios 
                // can take longer than default 100s
                Timeout = timeout
            };
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _httpClient.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Make request using reverse proxy
        /// </summary>
        /// <returns></returns>
        public virtual async Task<ApiResult> InvokeServiceAsync(HttpMethod httpMethod, string application, string service, string method, Dictionary<string, string> headers = null, string content = null)
        {
            return await InvokeServiceAsAsyncHelper(httpMethod, application, service, method, headers, content).ConfigureAwait(false);
        }

        /// <summary>
        /// Make request Helper
        /// </summary>
        /// <returns></returns>
        private async Task<ApiResult> InvokeServiceAsAsyncHelper(HttpMethod httpMethod, string application, string service, string method, Dictionary<string, string> headers, string content)
        {
            var serviceUri = new Uri($"https://localhost:{_ReverseProxyPort}/{application}/{service}/");
            var apiUri = new UriBuilder(new Uri(serviceUri, $"api/{method}"));

            HttpRequestMessage request = new HttpRequestMessage
            {
                RequestUri = apiUri.Uri,
                Method = httpMethod
            };

            if (!string.IsNullOrEmpty(content))
            {
                request.Content = new StringContent(content, Encoding.UTF8, "application/json");
                request.Headers.Add("Content-type", "application/json");
            }

            if (headers != null)
            {
                foreach(var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);

                }
            }

            HttpResponseMessage response = null;
            try
            {
                response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            }
            catch (Exception)
            {
                throw;
            }

            var result = new ApiResult();
            ProxyResponse(result, response);

            return result;
        }

        /// <summary>
        /// Proxy response message
        /// </summary>
        /// <returns></returns>
        private async void ProxyResponse(ApiResult result, HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // Handle the case where content is empty and preserve the original error reason (else will result in json parse error)
            if (string.IsNullOrEmpty(content))
            {
                result.Error = !response.IsSuccessStatusCode;
                result.Message = response.ReasonPhrase;
            }
            else
            {
                try
                {
                    var responseObj = JObject.Parse(content);
                    var errorProp = (bool?)responseObj["error"];
                    var resultProp = responseObj["result"];
                    var isError = errorProp.HasValue && errorProp.Value;

                    if (response.IsSuccessStatusCode && !isError)
                    {
                        result.Result = resultProp;
                    }
                    else
                    {
                        result.Error = true;
                        string message = "Error response from service";

                        try
                        {
                            if (isError)
                            {
                                message = (string)responseObj["message"];
                            }
                            // handle the case that the exception came directly from WebAPI
                            else if (responseObj["ExceptionMessage"] != null)
                            {
                                message = (string)responseObj["ExceptionMessage"];
                            }
                            else
                            {
                                message = (string)responseObj["Message"];
                            }
                        }
                        catch
                        {
                            // ignore errors
                        }

                        result.Message = message;

                        if (isError && resultProp != null)
                        {
                            result.Result = resultProp;
                        }
                    }
                }
                catch (Exception e)
                {
                    result.Error = true;
                    result.Message = $"Unable to parse content. Error={e.Message}";
                }
            }
        }
    }
}
