// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

using Microsoft.Azure.Services.AppAuthentication;
using ScenarioTester;
using System;
using DataXScenarios;
using Newtonsoft.Json;

namespace DataX.ServerScenarios
{
    /// <summary>
    /// Partial class defined such that the steps can be defined for the job 
    /// </summary>
    public partial class DataXHost
    {
        /// <summary>
        /// Utility method to retrieve the flow configuration content payload from context
        /// </summary>
        /// <param name="helper"></param>
        /// <returns></returns>
        private static string GetFlowConfigContentJson(ContextHelper helper)
        {
            return helper.GetContextValue<string>(Context.FlowConfigContent);
        }

        /// <summary>
        /// Utility method to retrieve flow name from context
        /// </summary>
        /// <param name="helper"></param>
        /// <returns></returns>
        private static string GetFlowName(ContextHelper helper)
        {
            return helper.GetContextValue<string>(Context.FlowName);
        }

        [Step("acquireToken")]
        public static StepResult AcquireToken(ScenarioContext context)
        {
            var tokenProvider = new AzureServiceTokenProvider();
            ContextHelper helper = new ContextHelper(context);
            helper.GetS2SAccessTokenForProdMSAAsync().Wait();

            return new StepResult(
                success: !string.IsNullOrWhiteSpace(helper.GetContextValue<string>(Context.AuthToken)),
                description: nameof(AcquireToken), 
                result: "acquired a bearer token");
        }
        

        [Step("saveJob")]
        public static StepResult SaveJob(ScenarioContext context)
        {
            ContextHelper helper = new ContextHelper(context);
            var baseAddress = helper.CreateUrl(ScenarioUri.SaveFlow);
            string input = GetFlowConfigContentJson(helper);
            dynamic result = helper.DoHttpPostJsonObject(baseAddress, input);
            string flowName = result.result.name.ToString();
            helper.SetContextValue<string>(Context.FlowName, flowName);
            return new StepResult(
                success: !string.IsNullOrWhiteSpace(flowName),
                description: nameof(SaveJob),
                result: $"created a flow '{flowName}' ");
        }

        [Step("startJob")]
        public static StepResult StartJob(ScenarioContext context)
        {
            ContextHelper helper = new ContextHelper(context);
            var baseAddress = helper.CreateUrl(ScenarioUri.StartFlowJobs);
            dynamic result = helper.DoHttpPostJsonString(baseAddress, GetFlowName(helper));
            string startJobName = result.result.IsSuccess.ToString();
            helper.SetContextValue<string>(Context.StartJobName, startJobName);
            return new StepResult(
                success: !string.IsNullOrWhiteSpace(startJobName),
                description: nameof(StartJob),
                result: $"created configs for the flow: '{startJobName}' ");
        }

        [Step("generateConfigs")]
        public static StepResult GenerateConfigs(ScenarioContext context)
        {
            ContextHelper helper = new ContextHelper(context);
            var baseAddress = helper.CreateUrl(ScenarioUri.GenerateConfigs);
            dynamic result = helper.DoHttpPostJsonString(baseAddress, GetFlowName(helper));
            string generateConfigsRuntimeConfigFolder = result.result.Properties.runtimeConfigFolder.ToString();
            helper.SetContextValue<string>(Context.GenerateConfigsRuntimeConfigFolder, generateConfigsRuntimeConfigFolder);
            return new StepResult(
                success: !string.IsNullOrWhiteSpace(generateConfigsRuntimeConfigFolder),
                description: nameof(GenerateConfigs),
                result: $"created configs for the flow: '{generateConfigsRuntimeConfigFolder}' ");
        }

        [Step("restartJob")]
        public static StepResult RestartJob(ScenarioContext context)
        {
            ContextHelper helper = new ContextHelper(context);
            var baseAddress = helper.CreateUrl(ScenarioUri.RestartFlowJobs);
            dynamic result = helper.DoHttpPostJsonString(baseAddress, GetFlowName(helper));
            string restartJobsName = result.result.IsSuccess.ToString();
            helper.SetContextValue<string>(Context.RestartJobsName, restartJobsName);
            return new StepResult(
                success: !string.IsNullOrWhiteSpace(restartJobsName), 
                description: nameof(RestartJob), 
                result: $"created configs for the flow: '{restartJobsName}' ");
        }

        [Step("getFlow")]
        public static StepResult GetFlow(ScenarioContext context)
        {
            ContextHelper helper = new ContextHelper(context);
            var baseAddress = helper.CreateUrl($"{ScenarioUri.GetFlow}?{ScenarioUri.GetFlowParamName}={GetFlowName(helper)}");
            dynamic result = helper.DoHttpGet(baseAddress);
            string flowConfig = result.result.name.ToString();
            helper.SetContextValue<string>(Context.FlowConfig, flowConfig);
            return new StepResult(
                success: !string.IsNullOrWhiteSpace(flowConfig),
                description: nameof(GetFlow), 
                result: "acquired flow");
        }
    }
}
