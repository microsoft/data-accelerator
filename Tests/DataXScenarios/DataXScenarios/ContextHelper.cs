// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.ServerScenarios;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;
using ScenarioTester;
using System;
using System.Threading.Tasks;

namespace DataXScenarios
{
    /// <summary>
    /// Helper class for the scnearios
    /// </summary>
    public class ContextHelper
    {
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
        /// Tells whether a field is set in the current context
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        private bool isValueSet(string fieldName)
        {
            return context.ContainsKey(fieldName) && context[fieldName] != null;
        }

        /// <summary>
        /// Tries to fetch a new value in the context, creates the entry if not available and returns it
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="supplier"></param>
        /// <returns></returns>
        public T SetContextValue<T>(string fieldName, Func<T> supplier)
        {
            if (!isValueSet(fieldName))
            {
                context[fieldName] = supplier.Invoke();
            }
            return (T)context[fieldName];
        }

        public T GetContextValue<T>(string fieldName)
        {
            return (T)Convert.ChangeType(context[fieldName], typeof(T));        }

        public async Task GetS2SAccessTokenForProdMSAAsync()
        {
            await GetS2SAccessToken(context[Context.MicrosoftAuthority] as string, context[Context.ApplicationIdentifierUri] as string, context[Context.ApplicationId] as string, context[Context.SecretKey] as string);
        }

        private async Task GetS2SAccessToken(string authority, string resource, string clientId, string clientSecret)
        {
            var clientCredential = new ClientCredential(clientId, clientSecret);
            AuthenticationContext authContext = new AuthenticationContext(authority, false);
            AuthenticationResult authenticationResult = await authContext.AcquireTokenAsync(
                resource,  // the resource (app) we are going to access with the token
                clientCredential);  // the client credentials
            context[Context.AuthToken] = authenticationResult.AccessToken;
            context[Context.AuthResult] = authenticationResult;
            context[Context.AccessTokenType] = authenticationResult.AccessTokenType;
        }

        /// <summary>
        /// Creates a JSON post request based in the current context data
        /// </summary>
        /// <param name="baseAddress"></param>
        /// <param name="json"></param>
        /// <returns></returns>
        public dynamic DoHttpPostJson(string baseAddress, string json)
        {
            string jsonResult = Request.Post(baseAddress, RequestContent.EncodeAsJson(json), bearerToken: context[Context.AuthToken] as string, skipServerCertificateValidation: (bool)context[Context.SkipServerCertificateValidation]);
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
                    bearerToken: context[Context.AuthToken] as string, skipServerCertificateValidation: (bool)context[Context.SkipServerCertificateValidation]);
            return JObject.Parse(jsonResult);
        }

        /// <summary>
        /// Generates a base url based on context contents
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string CreateUrl(string path)
        {
            return $"{context[Context.ServiceUrl] as string}{path}";
        }

        /// <summary>
        /// Helper function to store the result of a executed step
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fieldName">The field name of the context</param>
        /// <param name="stepResult></param>
        /// <returns></returns>
        public StepResult SaveContextValueWithStepResult(string fieldName, string contextValue, bool success, string description, string result)
        {
            SetContextValue<string>(fieldName, () => contextValue);
            return new StepResult(success, description, result);
        }
    }
}
