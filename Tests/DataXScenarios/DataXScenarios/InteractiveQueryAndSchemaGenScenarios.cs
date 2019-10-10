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
            return helper.SetContextValue(Context.InferSchemaInputJson, () =>
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
            return helper.SetContextValue(Context.InitializeKernelJson, () =>
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
            return helper.SetContextValue(Context.DeleteKernelJson, () =>
                $"{{\"kernelId\": \"{kernelId}\", \"name\": \"{flowName}\"}}"
            );
        }

        [Step("inferSchema")]
        public static StepResult InferSchema(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.SchemaInferenceService/inputdata/inferschema";
            ContextHelper helper = new ContextHelper(context);
            dynamic result = helper.DoHttpPostJson(baseAddress, GetInferSchemaJson(helper));
            string inputSchema = JsonConvert.SerializeObject((string)result.result.Schema);
            return helper.SaveContextValueWithStepResult(Context.InputSchema, inputSchema, 
                success: !string.IsNullOrWhiteSpace(inputSchema),
                description: nameof(InferSchema),
                result: $"Inferring Schema '{inputSchema}' ");
        }

        [Step("initializeKernel")]
        public static StepResult InitializeKernel(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.InteractiveQueryService/kernel";
            ContextHelper helper = new ContextHelper(context);
            dynamic result = helper.DoHttpPostJson(baseAddress, GetInitializeKernelJson(helper));
            string kernelId = result.result.result;
            return helper.SaveContextValueWithStepResult(Context.KernelId, contextValue: kernelId, 
                success: !(string.IsNullOrWhiteSpace(kernelId) && (string) result.result.message==""),
                description: nameof(InitializeKernel),
                result: $"Initialize a kernel '{kernelId}' ");
        }

        [Step("refreshKernel")]
        public static StepResult RefreshKernel(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.InteractiveQueryService/kernel/refresh";
            ContextHelper helper = new ContextHelper(context);
            dynamic result = helper.DoHttpPostJson(baseAddress, GetInitializeKernelJson(helper));
            string kernelId = result.result.result;
            return helper.SaveContextValueWithStepResult(Context.KernelId, contextValue: kernelId, 
                success: !(string.IsNullOrWhiteSpace(context[Context.KernelId] as string) && (string)result.result.message == ""),
                description: nameof(RefreshKernel),
                result: $"Refresh the kernel '{kernelId}' ");
        }

        [Step("refreshSample")]
        public static StepResult RefreshSample(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.SchemaInferenceService/inputdata/refreshsample";
            ContextHelper helper = new ContextHelper(context);
            dynamic result = helper.DoHttpPostJson(baseAddress, GetInferSchemaJson(helper));
            return new StepResult(
                success: ((string)result.result).Contains("success"),
                description: nameof(RefreshSample),
                result: $"Refreshing Sample");
        }

        [Step("refreshSampleAndKernel")]
        public static StepResult RefreshSampleAndKernel(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.LiveDataService/inputdata/refreshsampleandkernel";
            ContextHelper helper = new ContextHelper(context);
            dynamic result = helper.DoHttpPostJson(baseAddress, GetInitializeKernelJson(helper));
            string kernelId = result.result.result;
            return helper.SaveContextValueWithStepResult(Context.KernelId, contextValue: kernelId, 
                success: !(string.IsNullOrWhiteSpace(kernelId) && (string)result.result.message == ""),
                description: nameof(RefreshSampleAndKernel),
                result: $"Refresh the sample and kernel '{kernelId}' ");
        }

        [Step("deleteKernel")]
        public static StepResult DeleteKernel(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.InteractiveQueryService/kernel/delete";
            ContextHelper helper = new ContextHelper(context);
            dynamic result = helper.DoHttpPostJson(baseAddress, GetDeleteKernelJson(helper));
            return new StepResult(
                success: ((string)result.result).Contains("Success"),
                description: nameof(DeleteKernel),
                result: $"Delete the kernel '{helper.GetContextValue<string>(Context.KernelId)}' ");
        }

    }
}