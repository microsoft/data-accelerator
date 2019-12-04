// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using DataX.Contract;
using DataX.Flow.Common;
using DataX.Flow.Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

namespace DataX.Flow.InteractiveQuery
{
    /// <summary>
    /// This is the class that gets called into when interactive query apis are called into from the frontend
    /// </summary>
    public class InteractiveQueryManager
    {
        private string _flowContainerName => _engineEnvironment.EngineFlowConfig.FlowContainerName;
        private const string _GarbageCollectBlobName = "kernelList.json";
        private EngineEnvironment _engineEnvironment;
        private readonly ILogger _logger;
        private const string _HDInsight = Config.ConfigDataModel.Constants.SparkTypeHDInsight;
        private const string _DataBricks = Config.ConfigDataModel.Constants.SparkTypeDataBricks;
        private readonly IConfiguration _configuration;

        public InteractiveQueryManager(ILogger logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _engineEnvironment = new EngineEnvironment(_configuration);
        }

        private string SetupSteps { get; set; } = string.Empty;

        /// <summary>
        /// This method gets called for api/kernel
        /// </summary>
        /// <param name="jObject">JObject gets passed from the frontend containing the required parameters for this method</param>
        /// <returns>Returns the KernelId plus a warning in case the normalization snippet contains columns that are not part of the schema as generated on success. Returns Error in case of error.</returns>
        public async Task<ApiResult> CreateAndInitializeKernel(JObject jObject)
        {
            var diag = jObject.ToObject<InteractiveQueryObject>();
            ApiResult response;

            if (_engineEnvironment.EngineFlowConfig == null)
            {
                response = await _engineEnvironment.GetEnvironmentVariables();
                if (response.Error.HasValue && response.Error.Value)
                {
                    _logger.LogError(response.Message);
                    return ApiResult.CreateError(response.Message);
                }
            }

            if (string.IsNullOrEmpty(diag.Name))
            {
                diag.Name = await _engineEnvironment.GetUniqueName(Helper.GetSecretFromKeyvaultIfNeeded(_engineEnvironment.EngineFlowConfig.SubscriptionId), diag.DisplayName);
            }

            var hashValue = Helper.GetHashCode(diag.UserName);
            string sampleDataPath = Helper.SetValueBasedOnSparkType(_engineEnvironment.EngineFlowConfig.SparkType,
                Path.Combine(_engineEnvironment.OpsSparkSamplePath, $"{diag.Name}-{hashValue}.json"),
                Helper.ConvertToDbfsFilePath(_engineEnvironment.OpsSparkSamplePath, $"{diag.Name}-{hashValue}.json"));

            response = await CreateAndInitializeKernelHelper(diag.InputSchema, diag.UserName, diag.Name, sampleDataPath, diag.NormalizationSnippet, diag.ReferenceDatas, diag.Functions);
            if (response.Error.HasValue && response.Error.Value)
            {
                _logger.LogError(response.Message);
                return ApiResult.CreateError(response.Message);
            }

            var result = response.Result;
            return ApiResult.CreateSuccess(result);
        }

        /// <summary>
        /// ExecuteQuery method is called into when the api/kernel/execute api is called from the frontend
        /// </summary>
        /// <param name="queryObject">The query object is passed in</param>
        /// <returns>Returns the result from actually executing the query.</returns>
        public async Task<ApiResult> ExecuteQuery(JObject queryObject)
        {
            var response = await ExecuteQueryHelper(queryObject);
            if (response.Error.HasValue && response.Error.Value)
            {
                _logger.LogError(response.Message);
                return ApiResult.CreateError(response.Message);
            }

            var result = response.Result.ToString();
            return ApiResult.CreateSuccess(result);
        }

        /// <summary>
        /// This method gets called when kernel/sampleinputfromquery api is called
        /// </summary>
        /// <param name="queryObject">QueryObject is passed in from the frontend</param>
        /// <returns>Returns the sample data from the input table</returns>
        public async Task<ApiResult> GetSampleInputFromQuery(JObject queryObject)
        {
            var response = await GetSampleInputFromQueryHelper(queryObject);
            if (response.Error.HasValue && response.Error.Value)
            {
                _logger.LogError(response.Message);
                return ApiResult.CreateError(response.Message);
            }

            var result = response.Result.ToString();
            return ApiResult.CreateSuccess(result);
        }

        /// <summary>
        /// This method deleted the list the kernels as passed in
        /// </summary>
        /// <param name="kernels">The list of kernels to be deleted</param>
        /// <param name="subscriptionId">subscriptionId as passed in from the frontend</param>
        /// <param name="flowName">flow name</param>
        /// <returns>Returns success or error message as the case maybe</returns>
        public async Task<ApiResult> DeleteKernelList(List<string> kernels, string flowName)
        {
            var response = await _engineEnvironment.GetEnvironmentVariables().ConfigureAwait(false);
            var subscriptionId = Helper.GetSecretFromKeyvaultIfNeeded(_engineEnvironment.EngineFlowConfig.SubscriptionId);
            if (response.Error.HasValue && response.Error.Value)
            {
                _logger.LogError(response.Message);
                return ApiResult.CreateError(response.Message);
            }

            foreach (var k in kernels)
            {
                await DeleteKernelHelper(subscriptionId, k, flowName).ConfigureAwait(false);
            }

            return ApiResult.CreateSuccess("success");
        }

        /// <summary>
        /// This method gets called for deleting individual kernel as the frontend navigation occurs away from a particular flow for cleanup
        /// </summary>
        /// <param name="subscriptionId">subscriptionId as passed in from the frontend</param>
        /// <param name="kernelId">kernelId as passed in from the frontend</param>
        /// <param name="flowName">flowName as passed in from the frontend</param>
        /// <returns>Returns success or failure as the case may be</returns>
        public async Task<ApiResult> DeleteKernel(string kernelId, string flowName)
        {
            var response = await _engineEnvironment.GetEnvironmentVariables().ConfigureAwait(false);
            var subscriptionId = Helper.GetSecretFromKeyvaultIfNeeded(_engineEnvironment.EngineFlowConfig.SubscriptionId);
            if (response.Error.HasValue && response.Error.Value)
            {
                _logger.LogError(response.Message);
                return ApiResult.CreateError(response.Message);
            }

            response = await DeleteKernelHelper(subscriptionId, kernelId, flowName).ConfigureAwait(false);
            if (response.Error.HasValue && response.Error.Value)
            {
                _logger.LogError(response.Message);
                return ApiResult.CreateError(response.Message);
            }

            var result = response.Result.ToString();
            return ApiResult.CreateSuccess(result);
        }

        /// <summary>
        /// This method gets called when kernel/refresh api is called. This deletes the existing kernel whether in bad state or not and created and initializes
        /// </summary>
        /// <param name="jObject">JObject is passed in as parameter with all the required values as needed by this method</param>
        /// <returns>Returns the KernelId plus a warning in case the normalization snippet contains columns that are not part of the schema as generated on success. Returns Error in case of error.</returns>
        public async Task<ApiResult> RecycleKernel(JObject jObject)
        {
            var diag = jObject.ToObject<InteractiveQueryObject>();

            var response = await RecycleKernelHelper(diag, false);
            if (response.Error.HasValue && response.Error.Value)
            {
                _logger.LogError(response.Message);
                return ApiResult.CreateError(response.Message);
            }

            var result = response.Result;
            return ApiResult.CreateSuccess(result);
        }

        /// <summary>
        /// This is the helper function that does the heavy lifting for refreshing the kernel
        /// </summary>
        /// <param name="diag">InteractiveQueryObject that gets passed in from frontend</param>
        /// <param name="isReSample">This function is also used for refreshing the sample as well. This bool decides what action to take - use the existing kernel or create a new one</param>
        /// <returns>>Returns the KernelId plus a warning in case the normalization snippet contains columns that are not part of the schema as generated on success. Returns Error in case of error.</returns>
        public async Task<ApiResult> RecycleKernelHelper(InteractiveQueryObject diag, bool isReSample)
        {
            try
            {
                ApiResult response;
                if (_engineEnvironment.EngineFlowConfig == null)
                {
                    response = await _engineEnvironment.GetEnvironmentVariables();
                    if (response.Error.HasValue && response.Error.Value)
                    {
                        _logger.LogError(response.Message);
                        return ApiResult.CreateError(response.Message);
                    }
                }

                if (string.IsNullOrEmpty(diag.Name))
                {
                    diag.Name = await _engineEnvironment.GetUniqueName(Helper.GetSecretFromKeyvaultIfNeeded(_engineEnvironment.EngineFlowConfig.SubscriptionId), diag.DisplayName);
                }

                KernelService kernelService = CreateKernelService(diag.Name);

                //Create the xml with the scala steps to execute to initialize the kernel
                var hashValue = Helper.GetHashCode(diag.UserName);
                var sampleDataPath = Helper.SetValueBasedOnSparkType(_engineEnvironment.EngineFlowConfig.SparkType,
                    Path.Combine(_engineEnvironment.OpsSparkSamplePath, $"{diag.Name}-{hashValue}.json"),
                    Helper.ConvertToDbfsFilePath(_engineEnvironment.OpsSparkSamplePath, $"{diag.Name}-{hashValue}.json"));
                
                DiagnosticInputhelper(diag.InputSchema, sampleDataPath, diag.NormalizationSnippet, diag.Name);

                response = await kernelService.RecycleKernelAsync(diag.KernelId, diag.UserName, diag.Name, _engineEnvironment.OpsBlobConnectionString, Path.Combine(_engineEnvironment.OpsDiagnosticPath, _GarbageCollectBlobName), SetupSteps, isReSample, diag.ReferenceDatas, diag.Functions);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return ApiResult.CreateError(ex.Message);
            }
        }

        private KernelService CreateKernelService(string flowName)
        {
            if (_engineEnvironment.EngineFlowConfig.SparkType == _DataBricks)
            {
                var databricksTokenSecret = $"secretscope://{_engineEnvironment.EngineFlowConfig.SparkKeyVaultName}/{flowName}-info-databricksToken";
                return new Databricks.DatabricksKernelService(_engineEnvironment.EngineFlowConfig, _engineEnvironment.SparkConnInfo, _logger, Helper.GetSecretFromKeyvaultIfNeeded(databricksTokenSecret));
            }
            else
            {
                return new HDInsight.HDInsightKernelService(_engineEnvironment.EngineFlowConfig, _engineEnvironment.SparkConnInfo, _logger);
            }
        }

        /// <summary>
        /// This is the API method that gets called from the front end on a regular cadence and will delete all the kernels that are more than 3 hours old
        /// </summary>
        /// <param name="flowName">flowName</param>
        /// <returns>Returns the result whether the list of kernels were deleted or not. We don't fail if one of the kernels fails to delete because it just does not exist</returns>
        public async Task<ApiResult> DeleteKernels(string flowName)
        {
            var response = await _engineEnvironment.GetEnvironmentVariables().ConfigureAwait(false);
            var subscriptionId = Helper.GetSecretFromKeyvaultIfNeeded(_engineEnvironment.EngineFlowConfig.SubscriptionId);
            if (response.Error.HasValue && response.Error.Value)
            {
                _logger.LogError(response.Message);
                return ApiResult.CreateError(response.Message);
            }
            KernelService kernelService = CreateKernelService(flowName);
            response = await kernelService.GarbageCollectListOfKernels(_engineEnvironment.OpsBlobConnectionString, Path.Combine(_engineEnvironment.OpsDiagnosticPath, _GarbageCollectBlobName)).ConfigureAwait(false);

            if (response.Error.HasValue && response.Error.Value)
            {
                _logger.LogError(response.Message);
                return ApiResult.CreateError(response.Message);
            }
            var result = response.Result.ToString();
            return ApiResult.CreateSuccess(result);
        }

        /// <summary>
        /// This is the API method that gets called from the front end on a regular cadence and will delete all the kernels that were created
        /// </summary>
        /// <param name="flowName">flowName</param>
        /// <returns>Returns the result whether the list of kernels were deleted or not. We don't fail if one of the kernels fails to delete because it just does not exist</returns>
        public async Task<ApiResult> DeleteAllKernels(string flowName)
        {
            var response = await _engineEnvironment.GetEnvironmentVariables().ConfigureAwait(false);
            if (response.Error.HasValue && response.Error.Value)
            {
                _logger.LogError(response.Message);
                return ApiResult.CreateError(response.Message);
            }
            KernelService kernelService = CreateKernelService(flowName);
            _logger.LogInformation("Deleting all Kernels...");
            response = await kernelService.GarbageCollectListOfKernels(_engineEnvironment.OpsBlobConnectionString, Path.Combine(_engineEnvironment.OpsDiagnosticPath, _GarbageCollectBlobName), true).ConfigureAwait(false);

            if (response.Error.HasValue && response.Error.Value)
            {
                _logger.LogError(response.Message);
                return ApiResult.CreateError(response.Message);
            }
            var result = response.Result.ToString();
            _logger.LogInformation("Deleted and cleared all Kernels successfully");
            return ApiResult.CreateSuccess(result);
        }

        /// <summary>
        /// CreateAndInitializeKernelHelper is the helper that does the heavy listing for creating and initializing the kernel
        /// </summary>
        /// <param name="rawSchema">rawSchema as passed in from the frontend</param>
        /// <param name="userId">userId as passed in from the frontend</param>
        /// <param name="flowId">flowId as passed in from the frontend</param>
        /// <param name="sampleDataPath">sampleDataPath where the sample data is stored</param>
        /// <param name="normalizationSnippet">normalizationSnippet as passed in from the frontend</param>
        /// <param name="referenceDatas">referenceDatas as passed in from the frontend</param>
        /// <param name="functions">functions as passed in from the frontend</param>
        /// <returns></returns>
        private async Task<ApiResult> CreateAndInitializeKernelHelper(string rawSchema, string userId, string flowId, string sampleDataPath, string normalizationSnippet, List<ReferenceDataObject> referenceDatas, List<FunctionObject> functions)
        {
            try
            {
                //Create the xml with the scala steps to execute to initialize the kernel                
                DiagnosticInputhelper(rawSchema, sampleDataPath, normalizationSnippet, flowId);

                KernelService kernelService = CreateKernelService(flowId);
                var response = await kernelService.GarbageCollectListOfKernels(_engineEnvironment.OpsBlobConnectionString, Path.Combine(_engineEnvironment.OpsDiagnosticPath, _GarbageCollectBlobName));

                response = await kernelService.CreateKernelAsync();
                if (response.Error.HasValue && response.Error.Value)
                {
                    return ApiResult.CreateError(response.Message);
                }
                string kernelId = response.Result.ToString();

                response = await kernelService.UpdateGarbageCollectKernelBlob(kernelId, userId, flowId, _engineEnvironment.OpsBlobConnectionString, Path.Combine(_engineEnvironment.OpsDiagnosticPath, _GarbageCollectBlobName), true);
                if (response.Error.HasValue && response.Error.Value)
                {
                    await kernelService.DeleteKernelAsync(kernelId);
                    return ApiResult.CreateError(response.Message);
                }

                if(_engineEnvironment.EngineFlowConfig.SparkType == Config.ConfigDataModel.Constants.SparkTypeDataBricks)
                {
                    kernelService.MountStorage(_engineEnvironment.EngineFlowConfig.OpsStorageAccountName, _engineEnvironment.EngineFlowConfig.SparkKeyVaultName, sampleDataPath, kernelId);
                }

                response = await kernelService.CreateandInitializeKernelAsync(kernelId, SetupSteps, false, referenceDatas, functions);
                if (response.Error.HasValue && response.Error.Value)
                {
                    _logger.LogError(response.Message);
                    return ApiResult.CreateError(response.Message);
                }
                else
                {
                    return ApiResult.CreateSuccess(response.Result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return ApiResult.CreateError(ex.Message);
            }
        }

        /// <summary>
        /// ExecuteQueryHelper is the function that gets called for executing the user code
        /// </summary>
        /// <param name="jObject">jObject as passed in from the frontend</param>
        /// <returns>Returns the result from the kernel after executing the query</returns>
        private async Task<ApiResult> ExecuteQueryHelper(JObject jObject)
        {
            // validate the Execute Query API parameters i.e. the query object
            if (jObject == null)
            {
                _logger.LogError("Query Object is null");
                return ApiResult.CreateError("Query Object is null");
            }

            var query = jObject.ToObject<InteractiveQueryObject>();

            ApiResult response = await _engineEnvironment.GetEnvironmentVariables();
            if (response.Error.HasValue && response.Error.Value)
            {
                _logger.LogError(response.Message);
                return ApiResult.CreateError(response.Message);
            }

            if (string.IsNullOrEmpty(Helper.GetSecretFromKeyvaultIfNeeded(_engineEnvironment.EngineFlowConfig.SubscriptionId)))
            {
                return ApiResult.CreateError("Subscription can't be null");
            }

            try
            {
                KernelService kernelService = CreateKernelService(query.Name);
                var result = await kernelService.ExecuteQueryAsync(query.Query, query.KernelId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return ApiResult.CreateError(ex.Message);
            }
        }

        /// <summary>
        /// GetSampleInputFromQueryHelper gets the sample from the input table in the user query
        /// </summary>
        /// <param name="jObject">jObject </param>
        /// <returns>Returns the sample result from the input from the user query being executed</returns>
        private async Task<ApiResult> GetSampleInputFromQueryHelper(JObject jObject)
        {
            // validate JObject which is the query object in this case - is not null or empty
            if (jObject == null)
            {
                _logger.LogError("Query Object is null");
                return ApiResult.CreateError("Query Object is null");
            }

            var query = jObject.ToObject<InteractiveQueryObject>();
            try
            {
                KernelService kernelService = CreateKernelService(query.Name);
                var result = await kernelService.GetSampleInputFromQueryAsync(query.Query, query.KernelId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return ApiResult.CreateError(ex.Message);
            }
        }

        /// <summary>
        /// This deletes the kernelId as specified
        /// </summary>
        /// <param name="subscriptionId">subscriptionId that is passed in from the frontend</param>
        /// <param name="kernelId">kernelId that needs to be deleted</param>
        /// <param name="flowName">flowName</param>
        /// <returns>Returns success or failure after the delete kernel api is called</returns>
        private async Task<ApiResult> DeleteKernelHelper(string subscriptionId, string kernelId, string flowName)

        {
            // validate KernelId can't be null
            if (string.IsNullOrEmpty(kernelId))
            {
                _logger.LogError("No Kernel found to delete");
                return ApiResult.CreateError("No Kernel found to delete");
            }

            try
            {
                KernelService kernelService = CreateKernelService(flowName);
                var result = await kernelService.DeleteKernelAsync(kernelId);
                if (!(result.Error.HasValue && result.Error.Value))
                {
                    //Passing in false as the last parameter since this is a delete call and the entry needs to be deleted from the blob
                    await kernelService.UpdateGarbageCollectKernelBlob(kernelId, "", "", _engineEnvironment.OpsBlobConnectionString, Path.Combine(_engineEnvironment.OpsDiagnosticPath, _GarbageCollectBlobName), false);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return ApiResult.CreateError(ex.Message);
            }
        }

        /// <summary>
        /// This is a helper function that generates the steps that need to be executed for initializing the kernel and ProcessedInput
        /// </summary>
        /// <param name="rawSchema">rawSchema as passed in from the frontend</param>
        /// <param name="sampleDataPath">Sample Data path where the sample data is stored</param>
        /// <param name="normalizationSnippet">normalizationSnippet as passed in from the frontend</param>
        private void DiagnosticInputhelper(string rawSchema, string sampleDataPath, string normalizationSnippet, string flowId)
        {
            List<string> columnNames = new List<string>();
            var normalizationSnippetLines = normalizationSnippet.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            columnNames.AddRange(normalizationSnippetLines);
            string finalNormalizationString = string.Empty;

            if (columnNames.Count == 0)
            {
                finalNormalizationString = "\"Raw.*\"";
            }
            else
            {
                finalNormalizationString = $"\"{columnNames[0]}\"";
                for (int i = 1; i < columnNames.Count; i++)
                {
                    finalNormalizationString = finalNormalizationString + $@",""{columnNames[i]}""";
                }
            }
            Dictionary<string, string> values = new Dictionary<string, string>
            {
                ["RawSchema"] = rawSchema,
                ["SampleDataPath"] = sampleDataPath,
                ["NormalizationSnippet"] = finalNormalizationString,
                ["BinName"] = TranslateBinNames(_engineEnvironment.EngineFlowConfig.BinaryName, _engineEnvironment.EngineFlowConfig.OpsStorageAccountName, _engineEnvironment.EngineFlowConfig.InteractiveQueryDefaultContainer),
                ["KernelDisplayName"] = _engineEnvironment.GenerateKernelDisplayName(flowId)
            };
            SetupSteps = Helper.TranslateOutputTemplate(TemplateGenerator.GetSetupStepsTemplate(), values);
        }

        /// <summary>
        /// This translates the path to the jars to be loaded for the interactive query experience
        /// </summary>
        /// <param name="jars">jars to load per cosmosdb</param>
        /// <param name="storageAccountName">The storage account from where these jars are to be loaded</param>
        /// <returns></returns>
        public static string TranslateBinNames(JArray jars, string storageAccountName, string interactiveQueryDefaultContainer)
        {

            string binaryNames = string.Empty;
            if (jars.Count > 0)
            {
                for (int i = 0; i < jars.Count; i++)
                {
                    binaryNames += $@"wasbs://{interactiveQueryDefaultContainer}@{storageAccountName}.blob.core.windows.net/{jars[i]}";
                    if (i != jars.Count - 1)
                    {
                        binaryNames += ",";
                    }
                }
            }
            return binaryNames;
        }
    }
}
