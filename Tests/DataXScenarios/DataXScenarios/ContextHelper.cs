// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.ServerScenarios;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScenarioTester;
using System;
using System.Text;
using System.Threading.Tasks;

namespace DataXScenarios
{
    /// <summary>
    /// Helper class for the scnearios
    /// </summary>
    public class ContextHelper
    {
        private const string ApplicationJson = "application/json";

        private ScenarioContext context;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="scenarioContext"></param>
        public ContextHelper(ScenarioContext scenarioContext)
        {
            context = scenarioContext;
        }

        /// <summary>
        /// Sets a value into the specified field name in the context
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="supplier"></param>
        /// <returns></returns>
        public T SetContextValue<T>(string fieldName, T value)
        {
            context[fieldName] = value;
            return GetContextValue<T>(fieldName);
        }

        /// <summary>
        /// Retrieves a value from context with the desired type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public T GetContextValue<T>(string fieldName)
        {
            return (T)Convert.ChangeType(context[fieldName], typeof(T));        
        }

        public async Task GetS2SAccessTokenForProdMSAAsync()
        {
            await GetS2SAccessToken(
                GetContextValue<string>(Context.MicrosoftAuthority),
                GetContextValue<string>(Context.ApplicationIdentifierUri),
                GetContextValue<string>(Context.ApplicationId),
                GetContextValue<string>(Context.SecretKey));
        }

        private async Task GetS2SAccessToken(string authority, string resource, string clientId, string clientSecret)
        {
            var clientCredential = new ClientCredential(clientId, clientSecret);
            AuthenticationContext authContext = new AuthenticationContext(authority, false);
            AuthenticationResult authenticationResult = await authContext.AcquireTokenAsync(
                resource,  // the resource (app) we are going to access with the token
                clientCredential);  // the client credentials

            SetContextValue(Context.AuthToken, authenticationResult.AccessToken);
            SetContextValue(Context.AuthResult, authenticationResult);
            SetContextValue(Context.AccessTokenType, authenticationResult.AccessTokenType);
        }

        /// <summary>
        /// Performs a HTTP POST call to the specified base address and provided content
        /// </summary>
        /// <param name="baseAddress"></param>
        /// <param name="requestContent"></param>
        /// <returns></returns>
        public dynamic DoHttpPost(string baseAddress, RequestContent requestContent)
        {
            return Request.Post(baseAddress, requestContent,
                bearerToken: GetContextValue<string>(Context.AuthToken),
                skipServerCertificateValidation: GetContextValue<bool>(Context.SkipServerCertificateValidation));
        }

        /// <summary>
        /// Creates a JSON post request for an object representation based in the current context data
        /// </summary>
        /// <param name="baseAddress"></param>
        /// <param name="jsonObjectValue"></param>
        /// <returns></returns>
        public dynamic DoHttpPostJsonObject(string baseAddress, string jsonObjectValue)
        {
            string jsonResult = DoHttpPost(baseAddress, RequestContent.EncodeAsJson(JObject.Parse(jsonObjectValue), ApplicationJson));
            return JObject.Parse(jsonResult);
        }

        /// <summary>
        /// Creates a JSON post request for a string representation based in the current context data
        /// </summary>
        /// <param name="baseAddress"></param>
        /// <param name="jsonStringValue"></param>
        /// <returns></returns>
        public dynamic DoHttpPostJsonString(string baseAddress, string jsonStringValue)
        {
            string jsonResult = DoHttpPost(baseAddress, new RequestContent(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(jsonStringValue)), ApplicationJson));
            return JObject.Parse(jsonResult);
        }

        /// <summary>
        /// Do an http get request
        /// </summary>
        /// <param name="baseAddress"></param>
        /// <returns></returns>
        public dynamic DoHttpGet(string baseAddress)
        {
            string jsonResult = Request.Get(baseAddress,
                bearerToken: GetContextValue<string>(Context.AuthToken), 
                skipServerCertificateValidation: GetContextValue<bool>(Context.SkipServerCertificateValidation));
            return JObject.Parse(jsonResult);
        }

        /// <summary>
        /// Generates a base url based on context contents
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string CreateUrl(string path)
        {
            return $"{GetContextValue<string>(Context.ServiceUrl)}{path}";
        }
    }
}
