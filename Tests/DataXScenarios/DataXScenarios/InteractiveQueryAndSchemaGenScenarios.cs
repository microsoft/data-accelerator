// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
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
        [Step("inferSchema")]
        public static StepResult InferSchema(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.SchemaInferenceService/inputdata/inferschema";
            context[Context.InferSchemaInputJson] = $"{{\"name\": \"{context[Context.FlowName] as string}\", \"userName\": \"{context[Context.FlowName] as string}\", \"eventhubConnectionString\": \"{context[Context.EventhubConnectionString] as string}\", \"eventHubNames\": \"{context[Context.EventHubName] as string}\", \"inputType\": \"iothub\", \"seconds\": \"{context[Context.Seconds] as string}\", \"databricksToken\": \"{context[Context.DataBricksToken] as string}\"}}";
            string jsonResult = Request.Post(baseAddress,
                    RequestContent.EncodeAsJson(
                        JObject.Parse(context[Context.InferSchemaInputJson] as string)),
                    context[Context.AuthToken] as string);
            dynamic result = JObject.Parse(jsonResult);
            context[Context.InputSchema] = JsonConvert.SerializeObject((string)result.result.Schema);
            return new StepResult(!string.IsNullOrWhiteSpace(context[Context.InputSchema] as string),
                nameof(InferSchema),
                $"Inferring Schema '{context[Context.InputSchema]}' ");
        }

        [Step("initializeKernel")]
        public static StepResult InitializeKernel(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.InteractiveQueryService/kernel";
            
            context[Context.InitializeKernelJson] = $"{{\"name\": \"{context[Context.FlowName] as string}\", \"userName\": \"{context[Context.FlowName] as string}\", \"eventhubConnectionString\": \"{context[Context.EventhubConnectionString] as string}\", \"eventHubNames\": \"{context[Context.EventHubName] as string}\", \"inputType\": \"iothub\", \"inputSchema\": {context[Context.InputSchema] as string}, \"kernelId\": \"{context[Context.KernelId] as string}\", \"normalizationSnippet\": {context[Context.NormalizationSnippet] as string}, \"databricksToken\": \"{context[Context.DataBricksToken] as string}\"}}";
            
            string jsonResult = Request.Post(baseAddress,
                    RequestContent.EncodeAsJson(
                        JObject.Parse(context[Context.InitializeKernelJson] as string)),
                    context[Context.AuthToken] as string);

            dynamic result = JObject.Parse(jsonResult);
            context[Context.KernelId] = (string)result.result.result;

            context[Context.InitializeKernelJson] = $"{{\"name\": \"{context[Context.FlowName] as string}\", \"userName\": \"{context[Context.FlowName] as string}\", \"eventhubConnectionString\": \"{context[Context.EventhubConnectionString] as string}\", \"eventHubNames\": \"{context[Context.EventHubName] as string}\", \"inputType\": \"iothub\", \"inputSchema\": {context[Context.InputSchema] as string}, \"kernelId\": \"{context[Context.KernelId] as string}\", \"normalizationSnippet\": {context[Context.NormalizationSnippet] as string}, \"databricksToken\": \"{context[Context.DataBricksToken] as string}\"}}";
            
            return new StepResult(!(string.IsNullOrWhiteSpace(context[Context.KernelId] as string) && (string) result.result.message==""),
                nameof(InitializeKernel),
                $"Initialize a kernel '{context[Context.KernelId]}' ");
        }

        [Step("refreshKernel")]
        public static StepResult RefreshKernel(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.InteractiveQueryService/kernel/refresh";
            context[Context.InitializeKernelJson] = $"{{\"name\": \"{context[Context.FlowName] as string}\", \"userName\": \"{context[Context.FlowName] as string}\", \"eventhubConnectionString\": \"{context[Context.EventhubConnectionString] as string}\", \"eventHubNames\": \"{context[Context.EventHubName] as string}\", \"inputType\": \"iothub\", \"inputSchema\": {context[Context.InputSchema] as string}, \"kernelId\": \"{context[Context.KernelId] as string}\", \"normalizationSnippet\": {context[Context.NormalizationSnippet] as string}, \"databricksToken\": \"{context[Context.DataBricksToken] as string}\"}}";

            string jsonResult = Request.Post(baseAddress,
                    RequestContent.EncodeAsJson(
                        JObject.Parse(context[Context.InitializeKernelJson] as string)),
                    context[Context.AuthToken] as string);

            dynamic result = JObject.Parse(jsonResult);
            context[Context.KernelId] = (string)result.result.result;
            return new StepResult(!(string.IsNullOrWhiteSpace(context[Context.KernelId] as string) && (string)result.result.message == ""),
                nameof(RefreshKernel),
                $"Refresh the kernel '{context[Context.KernelId]}' ");
        }

        [Step("refreshSample")]
        public static StepResult RefreshSample(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.SchemaInferenceService/inputdata/refreshsample";
            context[Context.InferSchemaInputJson] = $"{{\"name\": \"{context[Context.FlowName] as string}\", \"userName\": \"{context[Context.FlowName] as string}\", \"eventhubConnectionString\": \"{context[Context.EventhubConnectionString] as string}\", \"eventHubNames\": \"{context[Context.EventHubName] as string}\", \"inputType\": \"iothub\", \"seconds\": \"{context[Context.Seconds] as string}\", \"databricksToken\": \"{context[Context.DataBricksToken] as string}\"}}";
            string jsonResult = Request.Post(baseAddress,
                    RequestContent.EncodeAsJson(
                        JObject.Parse(context[Context.InferSchemaInputJson] as string)),
                    context[Context.AuthToken] as string);
            dynamic result = JObject.Parse(jsonResult);
            //context[Context.InputSchema] = JsonConvert.SerializeObject((string)result.result.Schema);
            return new StepResult(((string)result.result).Contains("success"),
                nameof(RefreshSample),
                $"Refreshing Sample");
        }

        [Step("refreshSampleAndKernel")]
        public static StepResult RefreshSampleAndKernel(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.LiveDataService/inputdata/refreshsampleandkernel";
            context[Context.InitializeKernelJson] = $"{{\"name\": \"{context[Context.FlowName] as string}\", \"userName\": \"{context[Context.FlowName] as string}\", \"eventhubConnectionString\": \"{context[Context.EventhubConnectionString] as string}\", \"eventHubNames\": \"{context[Context.EventHubName] as string}\", \"inputType\": \"iothub\", \"inputSchema\": {context[Context.InputSchema] as string}, \"kernelId\": \"{context[Context.KernelId] as string}\", \"normalizationSnippet\": {context[Context.NormalizationSnippet] as string}, \"databricksToken\": \"{context[Context.DataBricksToken] as string}\"}}";

            string jsonResult = Request.Post(baseAddress,
                    RequestContent.EncodeAsJson(
                        JObject.Parse(context[Context.InitializeKernelJson] as string)),
                    context[Context.AuthToken] as string);

            dynamic result = JObject.Parse(jsonResult);
            context[Context.KernelId] = (string)result.result.result;
            return new StepResult(!(string.IsNullOrWhiteSpace(context[Context.KernelId] as string) && (string)result.result.message == ""),
                nameof(RefreshSampleAndKernel),
                $"Refresh the sample and kernel '{context[Context.KernelId]}' ");
        }


        [Step("deleteKernel")]
        public static StepResult DeleteKernel(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.InteractiveQueryService/kernel/delete";
            string json = JsonConvert.SerializeObject((string)context[Context.KernelId]);
            string jsonResult = Request.Post(baseAddress,
                    new RequestContent(Encoding.UTF8.GetBytes(json), "application/json"),
                    context[Context.AuthToken] as string);
            dynamic result = JObject.Parse(jsonResult);
            return new StepResult(((string)result.result).Contains("Success"),
                nameof(DeleteKernel),
                $"Delete the kernel '{context[Context.KernelId]}' ");
        }

    }
}