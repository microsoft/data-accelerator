// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.ServerScenarios;
using ScenarioTester;

namespace DataXScenarios
{
    /// <summary>
    /// Helper class for the scnearios
    /// </summary>
    public class Helper
    {
        /// <summary>
        /// Creating a helper function for constructing the InitializeKernelJson
        /// </summary>
        /// <param name="context">ScenarioContext</param>
        /// <returns></returns>
        public string GetInitializeKernelJson(ScenarioContext context)
        {
            return $"{{\"name\": \"{context[Context.FlowName] as string}\", \"userName\": \"{context[Context.FlowName] as string}\", \"eventhubConnectionString\": \"{context[Context.EventhubConnectionString] as string}\", \"eventHubNames\": \"{context[Context.EventHubName] as string}\", \"inputType\": \"iothub\", \"inputSchema\": {context[Context.InputSchema] as string}, \"kernelId\": \"{context[Context.KernelId] as string}\", \"normalizationSnippet\": {context[Context.NormalizationSnippet] as string}, \"databricksToken\": \"{context[Context.DataBricksToken] as string}\"}}";
        }

        public string GetDeleteKernelJson(ScenarioContext context)
        {
            return $"{{\"kernelId\": \"{context[Context.KernelId]}\", \"name\": \"{context[Context.FlowName]}\"}}";
        }
    }
}
