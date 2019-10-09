// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

using Microsoft.Azure.Services.AppAuthentication;
using ScenarioTester;
using System;
using DataXScenarios;

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
            ContextHelper helper = new ContextHelper(context);
            helper.GetS2SAccessTokenForProdMSAAsync().Wait();

            return new StepResult(!string.IsNullOrWhiteSpace(context[Context.AuthToken] as string),
                nameof(AcquireToken), "acquired a bearer token");
        }
        

        [Step("saveJob")]
        public static StepResult SaveJob(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.ManagementService/flow/save";
            ContextHelper helper = new ContextHelper(context);
            dynamic result = helper.DoHttpPostJson(baseAddress, context[Context.FlowConfigContent] as string);
            context[Context.FlowName] = (string)result.result.name;
            return new StepResult(!string.IsNullOrWhiteSpace(context[Context.FlowName] as string),
                nameof(SaveJob),
                $"created a flow '{context[Context.FlowName]}' ");
        }

        [Step("startJob")]
        public static StepResult StartJob(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.ManagementService/flow/startjobs";
            ContextHelper helper = new ContextHelper(context);
            dynamic result = helper.DoHttpPostJson(baseAddress, context[Context.FlowName] as string);
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
            ContextHelper helper = new ContextHelper(context);
            dynamic result = helper.DoHttpPostJson(baseAddress, context[Context.FlowName] as string);
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
            ContextHelper helper = new ContextHelper(context);
            dynamic result = helper.DoHttpPostJson(baseAddress, context[Context.FlowName] as string);
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
            ContextHelper helper = new ContextHelper(context);
            dynamic result = helper.DoHttpGet(baseAddress);
            context[Context.FlowConfig] = (string)result.result.name;
            return new StepResult(!string.IsNullOrWhiteSpace(context[Context.FlowConfig] as String),
                nameof(GetFlow), "acquired flow");
        }
    }
}
