// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using static System.FormattableString;

namespace ScenarioTester
{
    /// <summary>
    /// Content to be sent to the Request
    /// </summary>
    public class RequestContent
    {
        public readonly string ContentType;
        public readonly byte[] Data;

        /// <summary>
        /// Create content for a request.
        /// </summary>
        /// <param name="data">data in bytes</param>
        /// <param name="contentType">type of content</param>
        public RequestContent(byte[] data, string contentType)
        {
            Data = data;
            ContentType = contentType;
        }

        /// <summary>
        /// Create a <see cref="RequestContent"/> as a json paylod. 
        /// </summary>
        /// <param name="payload">The object to encode as json</param>
        /// <param name="contentType">optional content type; defaults to 'application/json'</param>
        /// <returns><see cref="RequestContent"/></returns>
        public static RequestContent EncodeAsJson(object payload, string contentType = "application/json")
        {
            return new RequestContent(
                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)),
                contentType
            );
        }
    }

    /// <summary>
    /// Utilities for handling requests to an API server.
    /// </summary>
    public class Request
    {
        /// <summary>
        /// Default request timeout of 20 minutes
        /// </summary>
        private const int _RequestTimeout = 1000 * 60 * 20;

        /// <summary>
        /// Helper function to do an HTTP GET.
        /// </summary>
        /// <param name="url">URL to GET response from</param>
        /// <param name="bearerToken">Optional bearer token to pass to the request Auth header</param>
        /// <param name="skipServerCertificateValidation">If true, skips server certificate validation</param>
        /// <returns>The response for the request as a string</returns>
        public static string Get(string url, string bearerToken = null, bool skipServerCertificateValidation = false)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            if (skipServerCertificateValidation)
            {
                req.ServerCertificateValidationCallback = (message, cert, chain, errors) => { return true; };
            }
            req.Method = "GET";
            req.Timeout = _RequestTimeout;
            if (bearerToken != null)
            {
                req.Headers.Add("Authorization", $"Bearer {bearerToken}");
            }
            req.Headers.Add("Content-type", "application/json");

            try
            {
                var resp = req.GetResponse();
                using (var reader = new StreamReader(resp.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (WebException we)
            {
                string returned;
                if (we.Response is HttpWebResponse)
                {
                    returned = "Server returned " + ((HttpWebResponse)we.Response).StatusCode;
                }
                else
                {
                    returned = "Response is " + we.Response;
                }

                throw new Exception(
                    Invariant($"Error (${we.Message}) posting to: {url} {returned} {GetErrorResponse(we)}"),
                    we
                );
            }
        }

        /// <summary>
        /// Helper function to do an HTTP POST  with an optional set of headers to set.
        /// </summary>
        /// <param name="url">URL to POST request to</param>
        /// <param name="content"><see cref="RequestContent"/> to send to the server</param>
        /// <param name="bearerToken">Optional bearer token to pass to the request Auth header</param>
        /// <param name="additionalHeaders">Any additional headers to add to the request</param>
        /// <param name="skipServerCertificateValidation">If true, skips server certificate validation</param>
        /// <returns>The response for the request as a string</returns>
        public static string Post(string url, RequestContent content, string bearerToken = null, Dictionary<string, string> additionalHeaders = null, bool skipServerCertificateValidation = false)
        {
            return PostPut("POST", url, content, bearerToken, additionalHeaders, skipServerCertificateValidation);
        }

        /// <summary>
        /// Helper function to do an HTTP PUT with an optional set of headers to set.
        /// </summary>
        /// <param name="url">URL to PUT request to</param>
        /// <param name="content"><see cref="RequestContent"/> to send to the server</param>
        /// <param name="bearerToken">Optional bearer token to pass to the request Auth header</param>
        /// <param name="additionalHeaders">Any additional headers to add to the request</param>
        /// <param name="skipServerCertificateValidation">If true, skips server certificate validation</param>
        /// <returns>The response for the request as a string</returns>
        public static string Put(string url, RequestContent content, string bearerToken = null, Dictionary<string, string> additionalHeaders = null, bool skipServerCertificateValidation = false)
        {
            return PostPut("PUT", url, content, bearerToken, additionalHeaders, skipServerCertificateValidation);
        }

        private static string PostPut(string requestMethod, string url, RequestContent content, string bearerToken = null, Dictionary<string, string> additionalHeaders = null, bool skipServerCertificateValidation = false)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            if (skipServerCertificateValidation)
            {
                req.ServerCertificateValidationCallback = (message, cert, chain, errors) => { return true; };
            }
            req.Method = requestMethod;
            req.Timeout = _RequestTimeout;
            req.ContentLength = content.Data.Length;
            req.ContentType = content.ContentType;
            if (bearerToken != null)
            {
                req.Headers.Add("Authorization", $"Bearer {bearerToken}");
            }
            if (additionalHeaders != null && additionalHeaders.Count > 0)
            {
                foreach (var headerValue in additionalHeaders)
                {
                    req.Headers.Add(headerValue.Key, headerValue.Value);
                }
            }

            using (var reqStream = req.GetRequestStream())
            {
                reqStream.Write(content.Data, 0, content.Data.Length);
            }
            try
            {
                var resp = req.GetResponse();
                using (var reader = new StreamReader(resp.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (WebException we)
            {
                string returned;
                if (we.Response is HttpWebResponse)
                {
                    returned = "Server returned " + ((HttpWebResponse)we.Response).StatusCode;
                }
                else
                {
                    returned = "Response is " + we.Response;
                }

                throw new Exception(
                    Invariant($"Error (${we.Message}) posting to: {url} {returned} {GetErrorResponse(we)}"),
                    we
                );
            }
        }

        private static string GetErrorResponse(WebException we)
        {
            using (var reader = new StreamReader(we.Response.GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
