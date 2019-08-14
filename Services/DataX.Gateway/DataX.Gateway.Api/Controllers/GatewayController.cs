// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Gateway.Contract;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Microsoft.ServiceFabric.Services.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DataX.Contract;
using DataX.Utilities.KeyVault;
using DataX.ServiceHost.ServiceFabric;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;

namespace DataX.Gateway.Api.Controllers
{
    [RoutePrefix("api")]
    [Authorize]
    public class GatewayController : ApiController
    {
        private static readonly HttpClient _HttpClient;
        private const string _UserNameHeader = Constants.UserNameHeader;
        private const string _UserIdHeader = Constants.UserIdHeader;
        private const string _UserGroupsHeader = Constants.UserGroupsHeader;
        private const string _UserRolesHeader = Constants.UserRolesHeader;
        private const string _ReverseProxyPort = Constants.ReverseProxyPort;
        private const int _DefaultHttpTimeoutSecs = Constants.DefaultHttpTimeoutSecs;
        private static readonly string _ReverseProxySslThumbprint;
        private static readonly string[] _AllowedUserRoles;
        private static readonly HashSet<string> _ClientWhitelist = new HashSet<string>();
        private static readonly ILogger _StaticLogger;
        private static readonly bool _IsUserInfoLoggingEnabled;

        private struct RequestDescriptor
        {
            public string Application;

            public string Body;

            public Dictionary<string, string> Headers;

            public HttpMethod HttpMethod;

            public string Method;

            public string QueryString;

            public string Service;
        };

        static GatewayController()
        {
            WinHttpHandler handler = new WinHttpHandler();

            // We need to set timeout for handler first
            // Setting timeout for httpClient alone is not good enough
            var timeout = TimeSpan.FromSeconds(_DefaultHttpTimeoutSecs);
            handler.SendTimeout = timeout;
            handler.ReceiveDataTimeout = timeout;
            handler.ReceiveHeadersTimeout = timeout;

            _HttpClient = new HttpClient(handler)
            {
                // Default http timeout is 100s, increase it to 4 min since few key mainline scenarios 
                // can take longer than default 100s
                Timeout = timeout
            };
            // Attach remote cert validation to ignore self-signed ssl cert error if reverseProxySslThumbprint is specified in the config 
            handler.ServerCertificateValidationCallback = ValidateRemoteCert;

            _ReverseProxySslThumbprint = GetReverseProxySslThumbprintFromConfig();
            _AllowedUserRoles = GetAllowedUserRolesFromConfig();
            var serviceKeyvaultName = ServiceFabricUtil.GetServiceKeyVaultName().Result.ToString();
            var configPackage = FabricRuntime.GetActivationContext().GetConfigurationPackageObject("Config");
            var serviceEnvironmenConfig = configPackage.Settings.Sections["ServiceEnvironment"];
            var appInsightsIntrumentationKey = serviceEnvironmenConfig.Parameters["AppInsightsIntrumentationKey"].Value;
            var testClientId = serviceEnvironmenConfig.Parameters["TestClientId"].Value;
            _IsUserInfoLoggingEnabled = IsUserInfoLoggingEnabled();
            _StaticLogger = new ApplicationInsightsLogger("GatewayILogger", new Microsoft.ApplicationInsights.TelemetryClient(new TelemetryConfiguration(KeyVault.GetSecretFromKeyvault(serviceKeyvaultName, appInsightsIntrumentationKey))), new ApplicationInsightsLoggerOptions());
            try
            {
                // Each secret needs to be a list of unique Ids in the format {ObjectId}.{TenantId}
                List<string> userIdList = KeyVault.GetSecretFromKeyvault(serviceKeyvaultName, testClientId).Split(new char[] { ',' }).ToList();
                foreach(string userId in userIdList)
                {
                    _ClientWhitelist.Add(userId);                    
                }
            }
            catch (Exception e)
            {
                // Do nothing in case the TestClientId is not set in the keyvault. This is set for testing purposes.
                var message = e.Message;
                _StaticLogger.LogError(e.Message);
            }
        }

        private async Task<ApiResult> Query(HttpRequestMessage request, string application, string service, string method, HttpMethod httpMethod)
        {
            var roles = ((ClaimsIdentity)User.Identity).Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value).ToList();
            var clientId = $"{ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value}.{ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value}";
            
            if (roles.Intersect(_AllowedUserRoles).Any() || _ClientWhitelist.Contains(clientId))
            {
                // Merge original headers with custom headers
                // Note: currently gets only the first value for a particular header
                Dictionary<string, string> headers = request.Headers != null
                    ? request.Headers
                        .Where(e => e.Key.StartsWith("X-"))
                        .ToDictionary(e => e.Key, e => e.Value.FirstOrDefault())
                    : new Dictionary<string, string>();

                foreach (KeyValuePair<string, string> entry in FetchUserHeaders())
                {
                    headers[entry.Key] = entry.Value;
                }

                RequestDescriptor descriptor = new RequestDescriptor
                {
                    HttpMethod = httpMethod,
                    Application = application,
                    Service = service,
                    Method = method,
                    QueryString = request.RequestUri.Query + string.Format("&Timeout={0}", _DefaultHttpTimeoutSecs),//set a timeout for reverseProxy
                    Body = httpMethod == HttpMethod.Post ? (await request.Content.ReadAsStringAsync()) : null,
                    Headers = headers
                };

                return await MakeRequestUsingReverseProxy(descriptor, new ServicePartitionKey());
            }
            else
            {
                return ApiResult.CreateError(string.Join(" or ", _AllowedUserRoles) + " role required.");
            }
        }

        [HttpGet]
        [Route("{application}/{service}/{*method}")]
        public async Task<ApiResult> QueryService(HttpRequestMessage request, string application, string service, string method)
        {
            return await Query(request, application, service, method, HttpMethod.Get);
        }

        [HttpPost]
        [Route("{application}/{service}/{*method}")]
        public async Task<ApiResult> QueryServiceWithPost(HttpRequestMessage request, string application, string service,
            string method)
        {
            return await Query(request, application, service, method, HttpMethod.Post);
        }

        /// <summary>
        /// Get user headers - UserName, UserId and UserRoles
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> FetchUserHeaders()
        {
            var headers = new Dictionary<string, string>();
            var userName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Upn) != null ? ClaimsPrincipal.Current.FindFirst(ClaimTypes.Upn).Value : ClaimsPrincipal.Current.FindFirst(ClaimTypes.Email)?.Value;
            headers.Add(_UserNameHeader, userName ?? string.Empty);            
            var userId = $"{ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value}.{ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value}";
            headers.Add(_UserIdHeader, userId ?? string.Empty);
            var roles = ((ClaimsIdentity)User.Identity).Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            headers.Add(_UserRolesHeader, JArray.FromObject(roles).ToString(Formatting.None));
            return headers;
        }

        /// <summary>
        /// Validate that the reverseProxySslThumbprint matches the reverse proxy cert thumbrint installed on ServiceFabric nodes 
        /// </summary>
        /// <returns></returns>
        private static bool ValidateRemoteCert(HttpRequestMessage sender, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // If cert is known, return true
            if (!string.IsNullOrEmpty(_ReverseProxySslThumbprint) && (certificate.Thumbprint.ToLower() == _ReverseProxySslThumbprint.ToLower()))
            {
                return true;
            }
            else
            {
                // If there are any ssl policy errors, log it
                if (sslPolicyErrors != System.Net.Security.SslPolicyErrors.None)
                {
                    ServiceEventSource.Current.Message($"ReverseProxySslError: {sslPolicyErrors.ToString()}");
                    _StaticLogger.LogInformation($"ReverseProxySslError: {sslPolicyErrors.ToString()}");
                }

                return sslPolicyErrors == System.Net.Security.SslPolicyErrors.None;
            }
        }

        /// <summary>
        /// Make request using reverse proxy
        /// </summary>
        /// <returns></returns>
        private async Task<ApiResult> MakeRequestUsingReverseProxy(RequestDescriptor descriptor, ServicePartitionKey servicePartitionKey)
        {
            ApiResult result = new ApiResult();
            Uri serviceUri = new Uri($"https://localhost:{_ReverseProxyPort}/{descriptor.Application}/{descriptor.Service}/");

            // This is logged mainly to track usage
            LogRequestEvent(serviceUri, descriptor);

            // Don't log queryString for 'groups' method as it contains sensitive info
            string queryString = (descriptor.Method == "groups" || descriptor.QueryString == null) ? string.Empty : descriptor.QueryString;

            // Generate request and receive response
            HttpRequestMessage request = CreateMicroserviceRequest(serviceUri, descriptor);

            HttpResponseMessage response = null;
            try
            {
                response = await _HttpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                // We have seen cases where SendAsync can fail. Log telemetry for those cases.

                Dictionary<string, string> properties = new Dictionary<string, string>();
                // Add endpoint uri for all requests except 'groups'
                if (!string.IsNullOrEmpty(request.RequestUri.AbsoluteUri) && descriptor.Method != "groups")
                {
                    properties.Add("endpointUri", request.RequestUri.AbsoluteUri);
                }

                ServiceEventSource.Current.Message($"Exception: {ex.ToString()}");
                _StaticLogger.LogError(ex, ex.Message, properties);
                throw;
            }

            ProxyResponse(result, response);

            // Log body for post calls/message if available. While this is not really responseCode value,
            // this is very useful for debugging.
            string responseCode = string.Format("StatusCode:{0}", response.StatusCode);
            if (!string.IsNullOrEmpty(result.Message))
            {
                responseCode = string.Format("{0}, Message:{1}", responseCode, result.Message);
            }
            // Log endpoint uri (e.g. http://10.0.0.5:20097) except groups calls.
            if (!string.IsNullOrEmpty(request.RequestUri.AbsoluteUri) && descriptor.Method != "groups")
            {
                responseCode = string.Format("{0}, EndpointUri:{1}", responseCode, request.RequestUri.AbsoluteUri);
            }

            ServiceEventSource.Current.Message($"Response: {responseCode}");
            _StaticLogger.LogInformation($"ResponseCode: {responseCode}; response.IsSuccessStatusCode: {response.IsSuccessStatusCode}");
            return result;
        }

        /// <summary>
        /// Get request header values 
        /// </summary>
        /// <returns></returns>
        private string GetRequestHeader(string name, HttpRequestHeaders headers)
        {
            if (headers == null || string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            string headerValue = string.Empty;
            headers.TryGetValues(name, out IEnumerable<string> headerValues);

            if (headerValues != null)
            {
                headerValue = string.Join(",", headerValues);
            }

            return headerValue;
        }

        /// <summary>
        /// Log telemetry to ETW and AppInsights
        /// </summary>
        /// <returns></returns>
        private void LogRequestEvent(Uri serviceUri, RequestDescriptor descriptor)
        {
            string userName = string.Empty;
            if (descriptor.Headers != null)
            {
                descriptor.Headers.TryGetValue(_UserNameHeader, out userName);
            }

            Dictionary<string, string> properties = new Dictionary<string, string>
            {
                { "method", descriptor.Method }
            };

            // Add username to logging if its enabled
            if (_IsUserInfoLoggingEnabled)
            {
                properties.Add("username", userName ?? string.Empty);
            }

            // Log non-groups method.
            if (descriptor.Method != "groups")
            {
                if (!string.IsNullOrEmpty(descriptor.QueryString))
                {
                    properties.Add("query", descriptor.QueryString);
                }
            }

            ServiceEventSource.Current.Message($"Request: {serviceUri.ToString()}");
            _StaticLogger.LogInformation(serviceUri.ToString(), properties);
        }

        /// <summary>
        /// Create request to other microservices 
        /// </summary>
        /// <returns></returns>
        private HttpRequestMessage CreateMicroserviceRequest(Uri serviceUri, RequestDescriptor descriptor)
        {
            // Construct URI base path
            UriBuilder apiUri = new UriBuilder(new Uri(serviceUri, $"api/{descriptor.Method}"));

            // Add query string to URI if one was given
            if (!string.IsNullOrEmpty(descriptor.QueryString))
            {
                // Have to slice the ? off
                apiUri.Query = descriptor.QueryString.Substring(1);
            }

            // Build HTTP request
            HttpRequestMessage request = new HttpRequestMessage
            {
                RequestUri = apiUri.Uri,
                Method = descriptor.HttpMethod
            };

            // Attach body if one was given. Assumes body is application/json.
            if (!string.IsNullOrEmpty(descriptor.Body))
            {
                request.Content = new StringContent(descriptor.Body, Encoding.UTF8, "application/json");
            }

            // Attach headers if any are given
            if (descriptor.Headers != null)
            {
                foreach (KeyValuePair<string, string> entry in descriptor.Headers)
                {
                    request.Headers.Add(entry.Key, entry.Value);
                }
            }

            return request;
        }

        /// <summary>
        /// Proxy response message
        /// </summary>
        /// <returns></returns>
        private async void ProxyResponse(ApiResult result, HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();

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
                    // Log telemetry
                    _StaticLogger.LogError(e, e.Message);
                }
            }
        }

        /// <summary>
        /// Get ReverseProxySslThumbprint from config 
        /// </summary>
        /// <returns></returns>
        private static string GetReverseProxySslThumbprintFromConfig()
        {
            string thumbprint = string.Empty;

            var configPackage = FabricRuntime.GetActivationContext().GetConfigurationPackageObject("Config");

            if (configPackage.Settings.Sections.Contains("SslCertsConfig"))
            {
                var sslCertsConfig = configPackage.Settings.Sections["SslCertsConfig"];
                if (sslCertsConfig.Parameters.Contains("ReverseProxySslThumbprint"))
                {
                    thumbprint = sslCertsConfig.Parameters["ReverseProxySslThumbprint"].Value;
                }
            }

            return thumbprint;
        }

        /// <summary>
        /// Get AllowedUserRoles from config 
        /// </summary>
        /// <returns></returns>
        private static string[] GetAllowedUserRolesFromConfig()
        {
            string allowedUserRoles = string.Empty;

            var configPackage = FabricRuntime.GetActivationContext().GetConfigurationPackageObject("Config");

            if (configPackage.Settings.Sections.Contains("RolesConfig"))
            {
                var rolesConfig = configPackage.Settings.Sections["RolesConfig"];
                if (rolesConfig.Parameters.Contains("AllowedUserRoles"))
                {
                    allowedUserRoles = rolesConfig.Parameters["AllowedUserRoles"].Value;
                }
            }
            return Regex.Replace(allowedUserRoles, @"\s+", "").Split(',');
        }

        /// <summary>
        /// Get the EnableUserInfoLogging setting from config
        /// </summary>
        /// <returns>bool indicating whether the user info logging is enabled or not</returns>
        private static bool IsUserInfoLoggingEnabled()
        {
            var configPackage = FabricRuntime.GetActivationContext().GetConfigurationPackageObject("Config");
            var serviceEnvironmenConfig = configPackage.Settings.Sections["ServiceEnvironment"];
            var enableUserInfoLoggingSetting = serviceEnvironmenConfig.Parameters["EnableUserInfoLogging"].Value;
            if(!string.IsNullOrEmpty(enableUserInfoLoggingSetting))
            {
                return enableUserInfoLoggingSetting.ToLower() == "true";
            }
            return false;
        }

    }
}
