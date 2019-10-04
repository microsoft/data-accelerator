// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using ScenarioTester;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Threading.Tasks;

namespace DataX.ServerScenarios
{
    /// <summary>
    /// Partial class defined such that the steps can be defined for the job 
    /// </summary>
    public partial class DataXHost
    {

        [Step("acquireToken")]
        public static StepResult AcquireToken(ScenarioContext context)
        {
            var tokenProvider = new AzureServiceTokenProvider();
            GetS2SAccessTokenForProdMSAAsync(context).Wait();

            return new StepResult(!string.IsNullOrWhiteSpace(context[Context.AuthToken] as string),
                nameof(AcquireToken), "acquired a bearer token");
        }
        static public async Task GetS2SAccessTokenForProdMSAAsync(ScenarioContext context)
        {
            await GetS2SAccessToken(context, context[Context.MicrosoftAuthority] as string, context[Context.ApplicationIdentifierUri] as string, context[Context.ApplicationId] as string, context[Context.SecretKey] as string);
        }

        static async Task GetS2SAccessToken(ScenarioContext context, string authority, string resource, string clientId, string clientSecret)
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

        [Step("saveJob")]
        public static StepResult SaveJob(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.ManagementService/flow/save";
            string jsonResult = Request.Post(baseAddress,
                    RequestContent.EncodeAsJson(
                        JObject.Parse(context[Context.FlowConfigContent] as string)),
                    bearerToken: context[Context.AuthToken] as string, sslTrust: (bool)context[Context.TrustSsl]);

            dynamic result = JObject.Parse(jsonResult);
            context[Context.FlowName] = (string)result.result.name;
            return new StepResult(!string.IsNullOrWhiteSpace(context[Context.FlowName] as string),
                nameof(SaveJob),
                $"created a flow '{context[Context.FlowName]}' ");
        }

        [Step("startJob")]
        public static StepResult StartJob(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.ManagementService/flow/startjobs";
            string json = JsonConvert.SerializeObject((string)context[Context.FlowName]);
            string jsonResult = Request.Post(baseAddress,
                    new RequestContent(Encoding.UTF8.GetBytes(json), "application/json"),
                    bearerToken: context[Context.AuthToken] as string, sslTrust: (bool)context[Context.TrustSsl]);
            dynamic result = JObject.Parse(jsonResult);
            context[Context.RestartJobsName] = (string)result.result.IsSuccess;
            string startJobName = context[Context.StartJobName] as string;

            return new StepResult(!string.IsNullOrWhiteSpace(startJobName),
                nameof(StartJob),
                $"created configs for the flow: '{startJobName}' ");
        }

        [Step("generateConfigs")]
        public static StepResult GenerateConfigs(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.ManagementService/flow/generateconfigs";
            string json = JsonConvert.SerializeObject((string)context[Context.FlowName]);
            string jsonResult = Request.Post(baseAddress,
                    new RequestContent(Encoding.UTF8.GetBytes(json), "application/json"),
                    bearerToken: context[Context.AuthToken] as string, sslTrust: (bool)context[Context.TrustSsl]);

            dynamic result = JObject.Parse(jsonResult);
            context[Context.GenerateConfigsRuntimeConfigFolder] = (string)result.result.Properties.runtimeConfigFolder;

            string generateConfigsRuntimeConfigFolder = context[Context.GenerateConfigsRuntimeConfigFolder] as string;

            return new StepResult(!string.IsNullOrWhiteSpace(generateConfigsRuntimeConfigFolder),
                nameof(GenerateConfigs),
                $"created configs for the flow: '{generateConfigsRuntimeConfigFolder}' ");
        }

        [Step("restartJob")]
        public static StepResult RestartJob(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.ManagementService/flow/restartjobs";
            string json = JsonConvert.SerializeObject((string)context[Context.FlowName]);
            string jsonResult = Request.Post(baseAddress,
                    new RequestContent(Encoding.UTF8.GetBytes(json), "application/json"),
                    bearerToken: context[Context.AuthToken] as string, sslTrust: (bool)context[Context.TrustSsl]);
            dynamic result = JObject.Parse(jsonResult);
            context[Context.RestartJobsName] = (string)result.result.IsSuccess;
            string restartJobsName = context[Context.RestartJobsName] as string;

            return new StepResult(!string.IsNullOrWhiteSpace(restartJobsName),
                nameof(RestartJob),
                $"created configs for the flow: '{restartJobsName}' ");
        }

        [Step("getFlow")]
        public static StepResult GetFlow(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.ManagementService/flow/get?flowName={context[Context.FlowName] as string}";

            string jsonResult = Request.Get(baseAddress,
                    bearerToken: context[Context.AuthToken] as string, sslTrust: (bool)context[Context.TrustSsl]);

            dynamic result = JObject.Parse(jsonResult);
            dynamic abc = (JObject)result.result;
            context[Context.FlowConfig] = (string)result.result.name;
            return new StepResult(!string.IsNullOrWhiteSpace(context[Context.FlowConfig] as String),
                nameof(GetFlow), "acquired flow");
        }
    }
}
