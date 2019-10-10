// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using ScenarioTester;
using Newtonsoft.Json;
using DataXScenarios;

namespace DataX.ServerScenarios
{
    /// <summary>
    /// Partial class defined such that the steps can be defined for the job 
    /// </summary>
    public partial class DataXHost
    {
        /// <summary>
        /// Creating a helper function for constructing GetInferSchemaJson
        /// </summary>
        /// <returns></returns>
        public static string GetInferSchemaJson(ContextHelper helper)
        {
            string flowName = helper.GetContextValue<string>(Context.FlowName);
            string eventHubConnectionString = helper.GetContextValue<string>(Context.EventhubConnectionString);
            string eventHubName = helper.GetContextValue<string>(Context.EventHubName);
            string seconds = helper.GetContextValue<string>(Context.Seconds);
            return helper.SetContextValue(Context.InferSchemaInputJson,
                $"{{\"name\": \"{flowName}\", \"userName\": \"{flowName}\", \"eventhubConnectionString\": \"{eventHubConnectionString}\", \"eventHubNames\": \"{eventHubName}\", \"inputType\": \"iothub\", \"seconds\": \"{seconds}\"}}"
            );
        }

        /// <summary>
        /// Creating a helper function for constructing the InitializeKernelJson
        /// </summary>
        /// <returns></returns>
        public static string GetInitializeKernelJson(ContextHelper helper)
        {
            string flowName = helper.GetContextValue<string>(Context.FlowName);
            string eventHubConnectionString = helper.GetContextValue<string>(Context.EventhubConnectionString);
            string eventHubName = helper.GetContextValue<string>(Context.EventHubName);
            string inputSchema = helper.GetContextValue<string>(Context.InputSchema);
            string kernelId = helper.GetContextValue<string>(Context.KernelId);
            string normalizationSnippet = helper.GetContextValue<string>(Context.NormalizationSnippet);
            return helper.SetContextValue(Context.InitializeKernelJson,
                $"{{\"name\": \"{flowName}\", \"userName\": \"{flowName}\", \"eventhubConnectionString\": \"{eventHubConnectionString}\", \"eventHubNames\": \"{eventHubName}\", \"inputType\": \"iothub\", \"inputSchema\": {inputSchema}, \"kernelId\": \"{kernelId}\", \"normalizationSnippet\": {normalizationSnippet}}}"
            );
        }

        /// <summary>
        /// Create a helper function for constructing the DeleteKernelJson
        /// </summary>
        /// <returns></returns>
        public static string GetDeleteKernelJson(ContextHelper helper)
        {
            string kernelId = helper.GetContextValue<string>(Context.KernelId);
            string flowName = helper.GetContextValue<string>(Context.FlowName);
            return helper.SetContextValue(Context.DeleteKernelJson,
                $"{{\"kernelId\": \"{kernelId}\", \"name\": \"{flowName}\"}}"
            );
        }

        [Step("inferSchema")]
        public static StepResult InferSchema(ScenarioContext context)
        {
            ContextHelper helper = new ContextHelper(context);
            var baseAddress = helper.CreateUrl("/api/DataX.Flow/Flow.SchemaInferenceService/inputdata/inferschema");
            dynamic result = helper.DoHttpPostJson(baseAddress, GetInferSchemaJson(helper));
            string inputSchema = JsonConvert.SerializeObject((string)result.result.Schema);
            helper.SetContextValue<string>(Context.InputSchema, inputSchema);
            return new StepResult(
                success: !string.IsNullOrWhiteSpace(inputSchema),
                description: nameof(InferSchema),
                result: $"Inferring Schema '{inputSchema}' ");
        }

        [Step("initializeKernel")]
        public static StepResult InitializeKernel(ScenarioContext context)
        {
            ContextHelper helper = new ContextHelper(context);
            var baseAddress = helper.CreateUrl("/api/DataX.Flow/Flow.InteractiveQueryService/kernel");
            dynamic result = helper.DoHttpPostJson(baseAddress, GetInitializeKernelJson(helper));
            string kernelId = (string)result.result.result;
            string message = (string)result.result.message;
            helper.SetContextValue<string>(Context.KernelId, kernelId);
            return new StepResult(
                success: !(string.IsNullOrWhiteSpace(kernelId) && message == ""),
                description: nameof(InitializeKernel),
                result: $"Initialize a kernel '{kernelId}' ");
        }

        [Step("refreshKernel")]
        public static StepResult RefreshKernel(ScenarioContext context)
        {
            ContextHelper helper = new ContextHelper(context);
            var baseAddress = helper.CreateUrl("/api/DataX.Flow/Flow.InteractiveQueryService/kernel/refresh");
            dynamic result = helper.DoHttpPostJson(baseAddress, GetInitializeKernelJson(helper));
            string kernelId = (string)result.result.result;
            string message = (string)result.result.message;
            helper.SetContextValue<string>(Context.KernelId, kernelId);
            return new StepResult(
                success: !(string.IsNullOrWhiteSpace(kernelId) && message == ""),
                description: nameof(RefreshKernel),
                result: $"Refresh the kernel '{kernelId}' ");
        }

        [Step("refreshSample")]
        public static StepResult RefreshSample(ScenarioContext context)
        {
            ContextHelper helper = new ContextHelper(context);
            var baseAddress = helper.CreateUrl("/api/DataX.Flow/Flow.SchemaInferenceService/inputdata/refreshsample");
            dynamic result = helper.DoHttpPostJson(baseAddress, GetInferSchemaJson(helper));
            string response = (string)result.result;
            return new StepResult(
                success: response.Contains("success"),
                description: nameof(RefreshSample),
                result: $"Refreshing Sample");
        }

        [Step("refreshSampleAndKernel")]
        public static StepResult RefreshSampleAndKernel(ScenarioContext context)
        {
            ContextHelper helper = new ContextHelper(context);
            var baseAddress = helper.CreateUrl("/api/DataX.Flow/Flow.LiveDataService/inputdata/refreshsampleandkernel");
            dynamic result = helper.DoHttpPostJson(baseAddress, GetInitializeKernelJson(helper));
            string kernelId = (string)result.result.result;
            string message = (string)result.result.message;
            helper.SetContextValue<string>(Context.KernelId, kernelId);
            return new StepResult(
                success: !(string.IsNullOrWhiteSpace(kernelId) && message == ""),
                description: nameof(RefreshSampleAndKernel),
                result: $"Refresh the sample and kernel '{kernelId}' ");
        }

        [Step("deleteKernel")]
        public static StepResult DeleteKernel(ScenarioContext context)
        {
            ContextHelper helper = new ContextHelper(context);
            var baseAddress = helper.CreateUrl("/api/DataX.Flow/Flow.InteractiveQueryService/kernel/delete");
            dynamic result = helper.DoHttpPostJson(baseAddress, GetDeleteKernelJson(helper));
            string response = (string)result.result;
            return new StepResult(
                success: response.Contains("Success"),
                description: nameof(DeleteKernel),
                result: $"Delete the kernel '{helper.GetContextValue<string>(Context.KernelId)}' ");
        }

    }
}