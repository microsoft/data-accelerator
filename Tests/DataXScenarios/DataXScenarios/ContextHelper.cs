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
        /// Tries to fetch a new json value in the context, creates the entry if not available and returns it
        /// </summary>
        /// <param name="jsonFieldName"></param>
        /// <param name="jsonProvider"></param>
        /// <returns></returns>
        private string getAndCacheJson(string jsonFieldName, Func<string> jsonProvider)
        {
            if (!isValueSet(jsonFieldName))
            {
                context[jsonFieldName] = jsonProvider.Invoke();
            }
            return (string)context[jsonFieldName];
        }

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
        /// Creating a helper function for constructing GetInferSchemaJson
        /// </summary>
        /// <returns></returns>
        public string GetInferSchemaJson()
        {
            return getAndCacheJson(Context.InferSchemaInputJson, () =>
                $"{{\"name\": \"{context[Context.FlowName] as string}\", \"userName\": \"{context[Context.FlowName] as string}\", \"eventhubConnectionString\": \"{context[Context.EventhubConnectionString] as string}\", \"eventHubNames\": \"{context[Context.EventHubName] as string}\", \"inputType\": \"iothub\", \"seconds\": \"{context[Context.Seconds] as string}\"}}"
            );
        }

        /// <summary>
        /// Creating a helper function for constructing the InitializeKernelJson
        /// </summary>
        /// <returns></returns>
        public string GetInitializeKernelJson()
        {
            return getAndCacheJson(Context.InitializeKernelJson, () =>
                $"{{\"name\": \"{context[Context.FlowName] as string}\", \"userName\": \"{context[Context.FlowName] as string}\", \"eventhubConnectionString\": \"{context[Context.EventhubConnectionString] as string}\", \"eventHubNames\": \"{context[Context.EventHubName] as string}\", \"inputType\": \"iothub\", \"inputSchema\": {context[Context.InputSchema] as string}, \"kernelId\": \"{context[Context.KernelId] as string}\", \"normalizationSnippet\": {context[Context.NormalizationSnippet] as string}}}"
            );
        }

        /// <summary>
        /// Create a helper function for constructing the DeleteKernelJson
        /// </summary>
        /// <returns></returns>
        public string GetDeleteKernelJson()
        {
            return getAndCacheJson(Context.DeleteKernelJson, () =>
                $"{{\"kernelId\": \"{context[Context.KernelId]}\", \"name\": \"{context[Context.FlowName]}\"}}"
            );
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

    }
}
