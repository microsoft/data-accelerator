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
        [Step("inferSchema")]
        public static StepResult InferSchema(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.SchemaInferenceService/inputdata/inferschema";
            ContextHelper helper = new ContextHelper(context);
            dynamic result = helper.DoHttpPostJson(baseAddress, helper.GetInferSchemaJson());
            context[Context.InputSchema] = JsonConvert.SerializeObject((string)result.result.Schema);
            return new StepResult(!string.IsNullOrWhiteSpace(context[Context.InputSchema] as string),
                nameof(InferSchema),
                $"Inferring Schema '{context[Context.InputSchema]}' ");
        }

        [Step("initializeKernel")]
        public static StepResult InitializeKernel(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.InteractiveQueryService/kernel";
            ContextHelper helper = new ContextHelper(context);
            dynamic result = helper.DoHttpPostJson(baseAddress, helper.GetInitializeKernelJson());
            context[Context.KernelId] = (string)result.result.result;
            return new StepResult(!(string.IsNullOrWhiteSpace(context[Context.KernelId] as string) && (string) result.result.message==""),
                nameof(InitializeKernel),
                $"Initialize a kernel '{context[Context.KernelId]}' ");
        }

        [Step("refreshKernel")]
        public static StepResult RefreshKernel(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.InteractiveQueryService/kernel/refresh";
            ContextHelper helper = new ContextHelper(context);
            dynamic result = helper.DoHttpPostJson(baseAddress, helper.GetInitializeKernelJson());
            context[Context.KernelId] = (string)result.result.result;
            return new StepResult(!(string.IsNullOrWhiteSpace(context[Context.KernelId] as string) && (string)result.result.message == ""),
                nameof(RefreshKernel),
                $"Refresh the kernel '{context[Context.KernelId]}' ");
        }

        [Step("refreshSample")]
        public static StepResult RefreshSample(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.SchemaInferenceService/inputdata/refreshsample";
            ContextHelper helper = new ContextHelper(context);
            dynamic result = helper.DoHttpPostJson(baseAddress, helper.GetInferSchemaJson());
            return new StepResult(((string)result.result).Contains("success"),
                nameof(RefreshSample),
                $"Refreshing Sample");
        }

        [Step("refreshSampleAndKernel")]
        public static StepResult RefreshSampleAndKernel(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.LiveDataService/inputdata/refreshsampleandkernel";
            ContextHelper helper = new ContextHelper(context);
            dynamic result = helper.DoHttpPostJson(baseAddress, helper.GetInitializeKernelJson());
            context[Context.KernelId] = (string)result.result.result;
            return new StepResult(!(string.IsNullOrWhiteSpace(context[Context.KernelId] as string) && (string)result.result.message == ""),
                nameof(RefreshSampleAndKernel),
                $"Refresh the sample and kernel '{context[Context.KernelId]}' ");
        }

        [Step("deleteKernel")]
        public static StepResult DeleteKernel(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.InteractiveQueryService/kernel/delete";
            ContextHelper helper = new ContextHelper(context);
            dynamic result = helper.DoHttpPostJson(baseAddress, helper.GetDeleteKernelJson());
            return new StepResult(((string)result.result).Contains("Success"),
                nameof(DeleteKernel),
                $"Delete the kernel '{context[Context.KernelId]}' ");
        }

    }
}