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

            Helper helper = new Helper();
            context[Context.InitializeKernelJson] = helper.GetInitializeKernelJson(context);
            string jsonResult = Request.Post(baseAddress,
                    RequestContent.EncodeAsJson(
                        JObject.Parse(context[Context.InitializeKernelJson] as string)),
                    context[Context.AuthToken] as string);

            dynamic result = JObject.Parse(jsonResult);
            context[Context.KernelId] = (string)result.result.result;

            context[Context.InitializeKernelJson] = helper.GetInitializeKernelJson(context);


            return new StepResult(!(string.IsNullOrWhiteSpace(context[Context.KernelId] as string) && (string) result.result.message==""),
                nameof(InitializeKernel),
                $"Initialize a kernel '{context[Context.KernelId]}' ");
        }

        [Step("refreshKernel")]
        public static StepResult RefreshKernel(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.InteractiveQueryService/kernel/refresh";
            
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
            
            string jsonResult = Request.Post(baseAddress,
                    RequestContent.EncodeAsJson(
                        JObject.Parse(context[Context.InferSchemaInputJson] as string)),
                    context[Context.AuthToken] as string);
            dynamic result = JObject.Parse(jsonResult);
            return new StepResult(((string)result.result).Contains("success"),
                nameof(RefreshSample),
                $"Refreshing Sample");
        }

        [Step("refreshSampleAndKernel")]
        public static StepResult RefreshSampleAndKernel(ScenarioContext context)
        {
            var baseAddress = $"{context[Context.ServiceUrl] as string}/api/DataX.Flow/Flow.LiveDataService/inputdata/refreshsampleandkernel";
            context[Context.InitializeKernelJson] = new Helper().GetInitializeKernelJson(context);

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
            context[Context.DeleteKernelJson] = new Helper().GetDeleteKernelJson(context);
            string jsonResult = Request.Post(baseAddress,
                    new RequestContent(Encoding.UTF8.GetBytes((string)context[Context.DeleteKernelJson]), "application/json"),
                    context[Context.AuthToken] as string);
            dynamic result = JObject.Parse(jsonResult);
            return new StepResult(((string)result.result).Contains("Success"),
                nameof(DeleteKernel),
                $"Delete the kernel '{context[Context.KernelId]}' ");
        }

    }
}