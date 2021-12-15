// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Contract;
using DataX.Flow.Common;
using DataX.Flow.Common.Models;
using DataX.Utilities.Blob;
using DataX.Utilities.KeyVault;
using DataX.Utility.KeyVault;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace DataX.Flow.InteractiveQuery
{
    /// <summary>
    /// This is the class that manages the kernel for interactive queries
    /// </summary>
    public abstract class KernelService
    {
        private steps _steps = null;
        private const int _MaxCount = 20;
        private string _setupStepsXml = "";
        private const string _QuerySeparator = "--DataXQuery--";

        /// <summary>
        /// The constructor for initializing the KernelService
        /// </summary>
        /// <param name="jobURLBase">jobURLBase based on the subscription used. Required for using Jupyter kernel Apis</param>
        /// <param name="keyvault">keyvault to use</param>
        /// <param name="secretPrefix">secret name prefix</param>
        /// <param name="logger">logger object passed so that telemetry can be added</param>
        public KernelService(FlowConfigObject flowConfig, SparkConnectionInfo connectionInfo, ILogger logger)
        {
            SparkType = flowConfig.SparkType;
            Logger = logger;
            FlowConfig = flowConfig;
        }

        protected FlowConfigObject FlowConfig { get; set; }

        protected SparkConnectionInfo SparkConnectionInfo { get; set; }

        protected ILogger Logger { get; set; }

        protected string SparkType { get; set; }


        /// <summary>
        /// CreateandInitializeKernelAsync - this the function that does the actual work of creating and initializing the kernel for the interactive query experience
        /// </summary>
        /// <param name="kernelId">kernelId</param>
        /// <param name="setupStepsXml">setupStepsXml - these are the intial steps to be executed to initialize the kernel</param>
        /// <param name="isReSample">isReSample - In case of resampling request from the UI, the actual intitialization is not needed</param>
        /// <param name="referenceDatas">referenceDatas: All reference data as specified by the user</param>
        /// <param name="functions">All UDFs and UDAFs as specified by the user</param>
        /// <returns>ApiResult which contains error or the kernelId (along with/without warning) as the case maybe</returns>
        public async Task<ApiResult> CreateandInitializeKernelAsync(string kernelId, string setupStepsXml, bool isReSample, List<ReferenceDataObject> referenceDatas, List<FunctionObject> functions)
        {
            _setupStepsXml = setupStepsXml;
            var response = await InitializeKernelAsync(kernelId, isReSample, referenceDatas, functions);
            if (response.Error.HasValue && response.Error.Value)
            {
                await DeleteKernelAsync(kernelId);
                Logger.LogError(response.Message);
                return ApiResult.CreateError(response.Message);
            }
            else
            {
                return ApiResult.CreateSuccess(response.Result);
            }
        }

        /// <summary>
        /// CreateKernelAsync - calls into the Rest api for creating the kernel
        /// </summary>
        /// <returns>ApiResult which contains kernelid</returns>
        public abstract Task<ApiResult> CreateKernelAsync();

        /// <summary>
        /// Returns the kernel given kernelId
        /// </summary>
        /// <param name="kernelId">Id of the kernel to return</param>
        /// <returns></returns>
        public abstract IKernel GetKernel(string kernelId);

        /// <summary>
        /// InitializeKernelAsync - this the helper function that does the actual work of creating and initializing the kernel for the interactive query experience
        /// </summary>
        /// <param name="kernelId">kernelId</param>
        /// <param name="isReSample">isReSample - In case of resampling request from the UI, the actual intitialization is not needed</param>
        /// <param name="referenceDatas">referenceDatas: All reference data as specified by the user</param>
        /// <param name="functions">All UDFs and UDAFs as specified by the user</param>
        /// <returns>ApiResult which contains error or the kernelId (along with/without warning) as the case maybe</returns>
        public async Task<ApiResult> InitializeKernelAsync(string kernelId, bool isReSample, List<ReferenceDataObject> referenceDatas, List<FunctionObject> functions)
        {
            try
            {
                // Attach to kernel
                IKernel kernel = GetKernel(kernelId);

                // Load and run the initialization steps
                var result = await Task.Run(() => LoadandRunSteps(kernel, isReSample, referenceDatas, functions));
                if(result.Error.HasValue && result.Error.Value)
                {
                    return ApiResult.CreateError(result.Message);
                }
                else
                {
                    return ApiResult.CreateSuccess(result.Result);
                }
            }
            catch(Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                return ApiResult.CreateError(ex.ToString());
            }
        }

        /// <summary>
        /// This is the function that gets the kernelList of kernels from the blob that are older than 3 hours and deletes them
        /// </summary>
        /// <param name="connectionString">Pass in the connection string for the Ops blob storage where there is a kernelList kernels mantained as they are created</param>        
        /// <param name="blobUri">blobUri: This is the Uri to the diagnostic blob where the kernelList all created kernels is maintained</param>
        /// <returns>Returns the result of delete call. If this fails because a kernel does not exist, it does not fail but continues to delete the remaining kernels</returns>
        public async Task<ApiResult> GarbageCollectListOfKernels(string connectionString, string blobUri, bool isDeleteAll = false)
        {
            string content = await Task.Run(() => BlobHelper.GetBlobContent(connectionString, blobUri));
            List<string> kernelList = new List<string>();
            if (!string.IsNullOrEmpty(content))
            {
                Dictionary<string, KernelProperties> kernelPropertiesDict = JsonConvert.DeserializeObject<Dictionary<string, KernelProperties>>(content);
                foreach (KeyValuePair<string, KernelProperties> kvp in kernelPropertiesDict)
                {
                    if (isDeleteAll)
                    {
                        kernelList.Add(kvp.Key);
                    }
                    else
                    {
                        //A list gets generated for all kernels that are more than 3 hours old. Front end needs to call this api on a regular cadence
                        if (DateTime.Now.AddHours(-3) > kvp.Value.CreateDate)
                        {
                            kernelList.Add(kvp.Key);
                        }
                    }
                }

                var response = await GarbageCollectKernelsHelper(kernelList);
                if (response.Error.HasValue && response.Error.Value)
                {
                    return response;
                }
                foreach (string kerId in kernelList)
                {
                    kernelPropertiesDict.Remove(kerId);
                }
                if (response.Error.HasValue && response.Error.Value)
                {
                    return ApiResult.CreateError(response.Message);
                }

                content = JsonConvert.SerializeObject(kernelPropertiesDict);
                await Task.Run(() => BlobHelper.SaveContentToBlob(connectionString, blobUri, content));
                return response;
            }
            else
            {
                return ApiResult.CreateSuccess("No Kernels to delete");
            }
        }

        /// <summary>
        /// Helper function to delete a kernelList of kernels. If a kernel has already been deleted, then don't return error message and instead continue with the rest of the kernelList and delete the rest of the kernels since the end goal is that the kernelList of kenrels should be deleted.
        /// </summary>
        /// <param name="kernelList">The kernelList of kernels</param>
        /// <returns></returns>
        public async Task<ApiResult> GarbageCollectKernelsHelper(List<string> deleteKernelList)
        {
            try
            {
                if (deleteKernelList.Count != 0)
                {
                    foreach (string kernelId in deleteKernelList)
                    {
                        var response = await DeleteKernelAsync(kernelId);
                        if (response.Error.HasValue && response.Error.Value)
                        {
                            if (!response.Message.Contains($@"Kernel does not exist: {kernelId}") && !response.Message.Contains("404 : Not Found"))
                            {
                                return ApiResult.CreateError(response.Message);
                            }
                        }
                    }
                    return ApiResult.CreateSuccess("Successfully deleted the kernelList of kernels");
                }
                else
                {
                    return ApiResult.CreateSuccess("No kernels to delete");
                }
            }
            catch (Exception ex)
            {
                return ApiResult.CreateError(ex.Message);
            }
        }

        private ApiResult LoadandRunSteps(IKernel kernel, bool isReSample, List<ReferenceDataObject> referenceDatas, List<FunctionObject> functions)
        {
            try
            {
                bool normalizationWarning = false;
                // Load steps from blob                
                XmlSerializer ser = new XmlSerializer(typeof(steps));
                using (StringReader sreader = new StringReader(_setupStepsXml))
                {
                    using (XmlReader reader = XmlReader.Create(sreader))
                    {
                        _steps = (steps)ser.Deserialize(reader);
                    }
                }

                string result = "";
                if (!isReSample)
                {
                    // Run the steps
                    for (int i = 0; i < _steps.Items.Count(); i++)
                    {
                        Logger.LogInformation($"steps.Items[{i}] ran successfully");
                        result = kernel.ExecuteCode(_steps.Items[i].Value);
                        LogErrors(result, _steps.Items[i].Value, $"InitializationStep[{i}]");
                        _steps.Items[i].Value = NormalizationSnippetHelper(ref i, ref normalizationWarning, result, _steps.Items);
                    }
                }
                else
                {
                    // Run the resample steps
                    for (int i = _steps.Items.Count() - 2; i < _steps.Items.Count(); i++)
                    {
                        Logger.LogInformation($"steps.Items[i].Value: _steps.Items[{i}]");
                        result = kernel.ExecuteCode(_steps.Items[i].Value);
                        var errors = CheckErrors(result);
                        if (!string.IsNullOrEmpty(errors))
                        {
                            Logger.LogError($"Initialization step: {_steps.Items[i].Value}. Resulting Error: {result}");
                        }
                        _steps.Items[i].Value = NormalizationSnippetHelper(ref i, ref normalizationWarning, result, _steps.Items);
                    }
                }

                // Now run the steps to load the reference data
                LoadReferenceData(kernel, referenceDatas);

                // Now load the UDFs and UDAFs
                LoadFunctions(kernel, functions);

                var error = CheckErrors(result);
                if (!string.IsNullOrEmpty(error))
                {
                    return ApiResult.CreateError("{'Error':'" + error + "'}");
                }
                else
                {
                    KernelResult responseObject = new KernelResult
                    {
                        Result = kernel.Id
                    };
                    if (normalizationWarning)
                    {
                        responseObject.Message = "Warning: Normalization query in the Input tab could not be applied, probably because some columns in the query are not part of the schema. Please update the schema or try auto-generating it using longer duration in the Input tab.";
                    }
                    else
                    {
                        responseObject.Message = string.Empty;
                    }

                    return ApiResult.CreateSuccess(JObject.FromObject(responseObject));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"ErrorMessage: {ex.Message} SetupSteps: {_setupStepsXml}");
                return ApiResult.CreateError(ex.ToString());
            }
        }

        private void LoadReferenceData(IKernel kernel, List<ReferenceDataObject> referenceDatas)
        {
            if (referenceDatas == null || referenceDatas.Count <= 0)
            {
                return;
            }

            for (int i = 0; i < referenceDatas.Count; i++)
            {
                string realPath = Helper.SetValueBasedOnSparkType(SparkType,
                    UDFPathResolver(referenceDatas[i].Properties.Path),
                    Helper.ConvertToDbfsFilePath(UDFPathResolver(referenceDatas[i].Properties.Path)));

                if (SparkType == DataX.Config.ConfigDataModel.Constants.SparkTypeDataBricks)
                {
                    MountStorage(FlowConfig.OpsStorageAccountName, FlowConfig.SparkKeyVaultName, realPath, kernel);
                }

                string code = $"spark.read.option(\"delimiter\", \"{(string.IsNullOrEmpty(referenceDatas[i].Properties.Delimiter) ? "," : referenceDatas[i].Properties.Delimiter)}\").option(\"header\", \"{referenceDatas[i].Properties.Header.ToString()}\").csv(\"{realPath}\").createOrReplaceTempView(\"{referenceDatas[i].Id}\"); print(\"done\");";
                string result = kernel.ExecuteCode(code);
                LogErrors(result, code, "LoadReferenceData");
            }
        }

        /// <summary>
        /// Loads functions in the kernel to enable function calls in interactive query mode
        /// </summary>
        /// <param name="kernel">Kernel to load functions into</param>
        /// <param name="functions">List of functions to load. These include UDFs, UDAFs and Azure functions</param>
        private void LoadFunctions(IKernel kernel, List<FunctionObject> functions)
        {
            if (functions == null || functions.Count <= 0)
            {
                return;
            }
            foreach (FunctionObject fo in functions)
            {
                switch (fo.TypeDisplay)
                {
                    case "UDF":
                    case "UDAF":
                        PropertiesUD properties = JsonConvert.DeserializeObject<PropertiesUD>(fo.Properties.ToString());
                        string realPath = UDFPathResolver(properties.Path);
                        string code = CreateLoadFunctionCode(realPath, SparkType, fo.TypeDisplay, fo.Id, properties);
                        string result = kernel.ExecuteCode(code);
                        LogErrors(result, code, "LoadFunctions");
                        break;
                    case "Azure Function":
                        // TODO
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Create code to load functions
        /// </summary>
        /// <param name="realPath">Path of jar</param>
        /// <param name="sparkType">spark type</param>
        /// <param name="functionType">function type</param>
        /// <param name="functionId">functionId</param>
        /// <param name="properties">Function properties</param>
        /// <returns>code to load functions</returns>
        public static string CreateLoadFunctionCode(string realPath, string sparkType, string functionType, string functionId, PropertiesUD properties)
        {
            string code = $"val jarPath = \"{realPath}\"\n";
            code += $"val mainClass = \"{properties.ClassName}\"\n";

            if (sparkType == DataX.Config.ConfigDataModel.Constants.SparkTypeDataBricks)
            {
                code += $"val jarFileUrl = datax.host.SparkJarLoader.addJarOnDriver(spark, jarPath, 0, true)\n";
            }
            else
            {
                code += $"val jarFileUrl = datax.host.SparkJarLoader.addJarOnDriver(spark, jarPath, 0, false)\n";
            }
            
            code += "spark.sparkContext.addJar(jarFileUrl)\n";

            if (functionType == "UDF")
            {
                code += $"datax.host.SparkJarLoader.registerJavaUDF(spark.udf, \"{functionId}\", mainClass, null)\n";
            }
            else if (functionType == "UDAF")
            {
                code += $"datax.host.SparkJarLoader.registerJavaUDAF(spark.udf, \"{functionId}\", mainClass)\n";
            }

            if (properties.Libs != null)
            {
                foreach (string lib in properties.Libs)
                {
                    code += $"datax.host.SparkJarLoader.addJar(spark, \"{lib}\")\n";
                }
            }
            code += "println(\"done\")";

            return code;
        }

        /// <summary>
        /// Helper function to modify the intialization step where normlaization snippet fails due to sample data not being in sync with the normalization snippet
        /// </summary>
        /// <param name="i">Pass in the index of the steps</param>
        /// <param name="normalizationWarning">pass in the paramter for the boolean which detemines if there was indeed an error while executing the normalization snippet</param>
        /// <param name="result">Pass in the result from the kernel post execution of the normalization step</param>
        /// <param name="stepsSteps">pass in the paramter with all the steps for intializing kenrel</param>
        /// <returns>Returns the new step content for the normalization step replacing the normalization content to just include 'Raw.*'</returns>
        private string NormalizationSnippetHelper(ref int i, ref bool normalizationWarning, string result, stepsStep[] stepsSteps)
        {
            if((i==3) && result.StartsWith("org.apache.spark.sql.AnalysisException: No such struct field"))
            {
                Regex r3 = new Regex(@"(selectExpr\s{0,}\(\s{0,}[^;]*?)(.createOrReplaceTempView)", RegexOptions.IgnoreCase);
                string str = stepsSteps[i].Value;
                MatchCollection m3 = r3.Matches(str);
                if (m3 != null && m3.Count > 0)
                {
                    foreach (Match m in m3)
                    {
                        try
                        {
                            var selectExprMatch = m.Groups[1].Value;
                            var item3 = str.Replace(selectExprMatch, @"selectExpr(""Raw.*"")");
                            i--;
                            normalizationWarning = true;
                            return item3;
                        }
                        catch (Exception)
                        {
                            Logger.LogError(new Exception("'selectExpr' not found in the normalization snippet in the Input tab"), "'selectExpr' not found in the normalization snippet in the Input tab");
                            throw new Exception("'selectExpr' not found in the normalization snippet in the Input tab");
                        }
                    }
                }
                else
                {

                    throw new Exception("'selectExpr' not found in the normalization snippet in the Input tab");
                }
                return str;
            }
            else
            {
                return stepsSteps[i].Value;
            }
        }

        /// <summary>
        /// ExecuteQueryAsync is the method that calls into the rest api for kernel for executing the code
        /// </summary>
        /// /// <param name="code">The code to be executed</param>
        /// <param name="kernelId">kernelId</param>
        /// <returns>ApiResult which contains the actual output from executing the query or the error as the case maybe</returns>
        public async Task<ApiResult> ExecuteQueryAsync(string code, string kernelId)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return ApiResult.CreateError("{\"Error\":\"Please select a query in the UI\"}");
                }

                if(code.Trim().StartsWith(_QuerySeparator))
                {
                    code = code.Replace(_QuerySeparator, "");
                }

                // Attach to kernel
                IKernel kernel = GetKernel(kernelId);

                // Remove Timewindow
                Regex timeRegex = new Regex(@"TIMEWINDOW\s{0,}\(\s{0,}.*\s{0,}\)", RegexOptions.IgnoreCase);
                Match match = timeRegex.Match(code);
                if (match.Success)
                {
                    code = code.Replace(match.Groups[0].Value, "");
                }

                // Handle WITH UPSERT
                Regex r1 = new Regex(@"\s{0,}([^;]*)WITH\s{1,}UPSERT\s{0,}([^;]*)", RegexOptions.IgnoreCase);
                Match m1 = r1.Match(code);
                if (m1.Success)
                {
                    string newQuery = m1.Groups[2].Value.Trim() + " = " + m1.Groups[1].Value.Trim() + "\r\n";
                    code = code.Replace(m1.Groups[0].Value, newQuery);
                }

                // Now check what kind of code we have, and execute
                code = code.TrimEnd(';');
                Regex r2 = new Regex("(.*)=([^;]*)");
                Match m2 = r2.Match(code);
                Regex r3 = new Regex(@"(\s{1,}CREATE TABLE\s{1,}[^;]*)", RegexOptions.IgnoreCase);
                Match m3 = r3.Match(" " + code);
                string g = Guid.NewGuid().ToString().Replace("-", "");
                string s = "";
                bool isTableCreateCommand = false;
                if (m2.Success)
                {
                    // SQL query assigning results to a table. Example: T1 = SELECT ...
                    string table = m2.Groups[1].Value.Trim();
                    s = $"val {table}Table = sql(\"{m2.Groups[2].Value}\"); {table}Table.toJSON.take({_MaxCount}).foreach(println); {table}Table.createOrReplaceTempView(\"{table}\")";
                }
                else if (m3.Success)
                {
                    // CREATE State query
                    isTableCreateCommand = true;
                    s = $"val results{g} = sql(\"{m3.Groups[1].Value.Trim()}\"); println(\"done\")";
                }
                else
                {
                    // SQL query without assigning results to a table. Example: SELECT ...
                    s = $"val results{g} = sql(\"{code}\"); results{g}.toJSON.take({_MaxCount}).foreach(println)";
                }

                // Execute code                
                string result = await Task.Run(() => kernel.ExecuteCode(ReplaceNewLineFeed(s)));

                // If the table already exists, then it is not an error. Mark this done.
                if (isTableCreateCommand && result != null && result.Contains("TableAlreadyExistsException"))
                {
                    result = "done";
                }

                if (!string.IsNullOrEmpty(result))
                {
                    return ConvertToJson(_MaxCount, result);
                }
                else
                {
                    return ApiResult.CreateError("{\"Error\":\"No Results\"}");
                }
            }
            catch (Exception ex)
            {
                return ApiResult.CreateError(ex.ToString());
            }
        }

        /// <summary>
        /// This is the function that gets the sample input from the query as specified by the user for the diagnostic experience
        /// </summary>
        /// <param name="kernelId">kernelId</param>
        /// <param name="code">The code to be executed</param>
        /// <returns>ApiResult which contains the actual sample input or the error as the case maybe</returns>
        public async Task<ApiResult> GetSampleInputFromQueryAsync(string kernelId, string code)
        {
            try
            {
                // Handle CREATE query
                if (code.ToLower().StartsWith("create "))
                {
                    return ApiResult.CreateSuccess("done");
                }

                // Attach to kernel
                IKernel kernel = GetKernel(kernelId);

                // Prep code to execute
                Regex r = new Regex(@"FROM\s*(\w+)");
                Match m = r.Match(code);
                string g = Guid.NewGuid().ToString().Replace("-", "");
                string s = "";
                if (m.Success)
                {
                    s = $"val results{g} = sql(\"SELECT * FROM {m.Groups[1].Value} LIMIT {_MaxCount}\"); results{g}.toJSON.take({_MaxCount}).foreach(println)";
                }

                string result = await Task.Run(() => kernel.ExecuteCode(ReplaceNewLineFeed(s)));
                LogErrors(result, ReplaceNewLineFeed(s), "GetSampleInputFromQueryAsync");
                if (!string.IsNullOrEmpty(result))
                {
                    return ConvertToJson(_MaxCount, result);
                }
                else
                {
                    return ApiResult.CreateError("{\"Error\":\"No Results\"}");
                }
            }
            catch (Exception ex)
            {
                return ApiResult.CreateError(ex.ToString());
            }
        }

        /// <summary>
        /// This method RecycleKernelAsync refreshes the kernel. deletes the old kernel and creates and initializes a new kernel.
        /// </summary>
        /// <param name="kernelId">kernelId</param>
        /// <param name="userId">userId</param>
        /// <param name="flowId">flowId</param>
        /// <param name="connectionString">SparkConnectionString to Ops is needed for resampling of the data and saving it to the blob</param>
        /// <param name="blobUri">blobUri to the blob where the sample data is stored</param>
        /// <param name="setupStepsXml">setupStepsXml contains the steps that are needed to run against the kernel</param>
        /// <param name="isReSample">boolean that helps identify if it is resample api call or refresh api call</param>
        /// <returns>Returns success (KernelId) or error as the case maybe as ApiResult</returns>
        public async Task<ApiResult> RecycleKernelAsync(string kernelId, string userId, string flowId, string connectionString, string blobUri, string setupStepsXml, bool isReSample, List<ReferenceDataObject> referenceDatas, List<FunctionObject> functions)
        {
            try
            {
                //If it is not a resample call, don't create and delete the kernel
                if (!isReSample)
                {
                    //Delete only if the kernelId has been passed in
                    if (!string.IsNullOrEmpty(kernelId))
                    {
                        var deleteResult = await DeleteKernelAsync(kernelId);
                    }
                    var response = await GarbageCollectListOfKernels(connectionString, blobUri);

                    response = await CreateKernelAsync();
                    if (response.Error.HasValue && response.Error.Value)
                    {
                        return ApiResult.CreateError(response.Message);
                    }
                    kernelId = response.Result.ToString();
                    response = await UpdateGarbageCollectKernelBlob(kernelId, userId, flowId, connectionString, blobUri, true);
                }
                return await CreateandInitializeKernelAsync(kernelId, setupStepsXml, isReSample, referenceDatas, functions);
            }
            catch (Exception ex)
            {
                return ApiResult.CreateError(ex.ToString());
            }
        }

        /// <summary>
        /// This is called for deleting the kernel by directly calling into the Rest Api's provided by the jupyter kernel
        /// </summary>
        /// <param name="kernelId">KernelId</param>
        /// <returns>Returns success or error as the case maybe as ApiResult</returns>
        public abstract Task<ApiResult> DeleteKernelAsync(string kernelId);

        /// <summary>
        /// This is a helper function that can be used for updating the blob both when delete and create kernels are called
        /// </summary>
        /// <param name="kernelId">KernelId is the parameter for the kernel that is being created and deleted</param>
        /// <param name="userId">userId</param>
        /// <param name="flowId">flowId</param>
        /// <param name="connectionString">Ops Connection string that is need to access the blob</param>
        /// <param name="blobUri">blobUri of the blob where the kernel data is stored</param>
        /// <param name="isCreate">IsCerate is the boolean which tells whether to delete or add an entry in the dictionry in the blob</param>
        /// <returns>Returns success or the error message as ApiResult as the case maybe</returns>
        public async Task<ApiResult> UpdateGarbageCollectKernelBlob(string kernelId, string userId, string flowId, string connectionString, string blobUri, bool isCreate)
        {
            try
            {
                string content = await Task.Run(() => BlobHelper.GetBlobContent(connectionString, blobUri));
                Dictionary<string, KernelProperties> kernelPropertiesDict = new Dictionary<string, KernelProperties>();
                if (!string.IsNullOrEmpty(content))
                {
                    kernelPropertiesDict = JsonConvert.DeserializeObject<Dictionary<string, KernelProperties>>(content);
                }
                if (isCreate)
                {
                    kernelPropertiesDict.Add(kernelId, new KernelProperties() { UserId = userId, FlowId = flowId, CreateDate = DateTime.Now });
                }
                else if (kernelPropertiesDict.Count > 0 && kernelPropertiesDict.ContainsKey(kernelId))
                {
                    kernelPropertiesDict.Remove(kernelId);
                }
                content = JsonConvert.SerializeObject(kernelPropertiesDict);
                await Task.Run(() => BlobHelper.SaveContentToBlob(connectionString, blobUri, content));
                return ApiResult.CreateSuccess("Updated Blob");
            }
            catch (Exception ex)
            {
                return ApiResult.CreateError(ex.Message);
            }
        }

        /// <summary>
        /// Used for initializing the header the kernel
        /// </summary>
        /// <param name="plainText">Encodes the username and password</param>
        /// <returns>Encoded username and password</returns>
        public string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        /// <summary>         
        /// Helper function to log errors along with the scala code as executed by the kernel
        /// </summary>        
        /// <param name="result">The actual kernel result on executeQuery</param>
        /// <param name="code">The query that is executed</param>
        /// <param name="step">A string that indicates which intialization step it is</param>
        private void LogErrors(string result, string code, string step)
        {
            var errors = CheckErrors(result);
            if (!string.IsNullOrEmpty(errors))
            {
                Logger.LogError($"Initialization step - {step}: Resulting Error: {errors}");
            }
        }

        /// <summary>
        /// Returning a json object
        /// </summary>
        /// <param name="maxCount">Maximum number of rows to return to the UI</param>
        /// <param name="result">The Result as returned by the kernel</param>
        /// <returns>ApiResult which contains the error message or the json formatted data as returned by kernel</returns>
        public static ApiResult ConvertToJson(int maxCount, string result)
        {
            var error = CheckErrors(result);
            if (!string.IsNullOrEmpty(error))
            {
                return ApiResult.CreateError("{\"Error\": \"" + error + "\"}");
            }
            else
            {
                try
                {
                    var lines = result.Split('\n').ToList();
                    Regex regex = new Regex(".*: org.apache.spark");
                    if (lines != null && lines.Count >= 0 && regex.Match(lines[lines.Count - 1]).Success)
                    {
                        lines.RemoveAt(lines.Count - 1);
                    }

                    var json = JArray.FromObject(lines);

                    return ApiResult.CreateSuccess(json.ToString());
                }
                catch(Exception)
                {
                    return ApiResult.CreateError("{\"Error\": \"" + result + "\"}");
                }
            }
        }

        /// <summary>
        /// CheckErrors is the function that is used check for errors on the message string as received from the kernel on executing the code. This identifies if the string is an error message
        /// </summary>
        /// <param name="result">result is the string that is returned from the kernel on executing scala code</param>
        /// <returns>Returns the result only in case there is error otherwise returns null</returns>
        private static string CheckErrors(string result)
        {
            Regex r = new Regex(@"or[^;]+line (\d+).*pos (\d+)", RegexOptions.IgnoreCase);
            Match m = r.Match(ReplaceNewLineFeed(result));
            if (m.Success)
            {
                return result;
            }

            return null;
        }

        private static string ReplaceNewLineFeed(string input)
        {
            return input.Replace("\r", " ").Replace("\n", " ");
        }

        /// <summary>
        /// UDFPathResolver resolves the keyvault uri and gets the real path 
        /// </summary>
        /// <param name="path">path</param>
        /// <returns>Returns a string </returns>
        private string UDFPathResolver(string path)
        {
            if (path != null && Config.Utility.KeyVaultUri.IsSecretUri(path))
            {
                SecretUriParser.ParseSecretUri(path, out string keyvalut, out string secret);
                var secretUri = KeyVault.GetSecretFromKeyvault(keyvalut, secret);

                return secretUri;
            }
            return path;
        }

        /// <summary>
        /// Mount container to DBFS 
        /// </summary>
        /// <param name="opsStorageAccountName">Storage account name</param>
        /// <param name="sparkKeyVaultName">Spark keyvault name</param>
        /// <param name="dbfsPath">DBFS path. Format dbfs:/mnt/livequery/..</param>
        /// <param name="kernelId">kernelId</param>
        public void MountStorage(string opsStorageAccountName, string sparkKeyVaultName, string dbfsPath, string kernelId)
        {
            // Attach to kernel
            IKernel kernel = GetKernel(kernelId);
            MountStorage(opsStorageAccountName, sparkKeyVaultName, dbfsPath, kernel);
        }

        /// <summary>
        /// Mount container to DBFS 
        /// </summary>
        /// <param name="opsStorageAccountName">Storage account name</param>
        /// <param name="sparkKeyVaultName">Spark keyvault name</param>
        /// <param name="dbfsPath">DBFS path. Format dbfs:/mnt/livequery/..</param>
        /// <param name="kernel">kernel</param>
        private void MountStorage(string opsStorageAccountName, string sparkKeyVaultName, string dbfsPath, IKernel kernel)
        {
            try
            {
                //Verify the container has been mounted to DBFS
                string verifyCode = $"dbutils.fs.ls(\"{dbfsPath}\")";
                var result = kernel.ExecuteCode(verifyCode);

                //If container is not mounted then mount it
                if (string.IsNullOrEmpty(result))
                {
                    string mountCode = CreateMountCode(dbfsPath, opsStorageAccountName, sparkKeyVaultName);
                    kernel.ExecuteCode(mountCode);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
            }
        }

        /// <summary>
        /// Create mount command to execute 
        /// </summary>
        /// <param name="dbfsPath">DBFS path. Format dbfs:/mnt/livequery/..</param>
        /// <param name="opsStorageAccountName">Ops Storage Account Name</param>
        /// <param name="sparkKeyVaultName">Spark KeyVault Name</param>
        /// <returns>Returns the mount code </returns>
        public static string CreateMountCode(string dbfsPath, string opsStorageAccountName, string sparkKeyVaultName)
        {
            Regex r = new Regex($"{Config.ConfigDataModel.Constants.PrefixDbfs}{Config.ConfigDataModel.Constants.PrefixDbfsMount}([a-zA-Z0-9-]*)/", RegexOptions.IgnoreCase);
            string containerName = r.Match(dbfsPath).Groups[1].Value;
            string mountCode = $"dbutils.fs.mount(" +
                $"source = \"wasbs://{containerName}@{opsStorageAccountName}.blob.core.windows.net/\", " +
                $"mountPoint = \"/{Config.ConfigDataModel.Constants.PrefixDbfsMount}/{containerName}\", " +
                $"extraConfigs = Map(" +
                    $"\"fs.azure.account.key.{opsStorageAccountName}.blob.core.windows.net\"->" +
                    $"dbutils.secrets.get(scope = \"{sparkKeyVaultName}\", key = \"{Config.ConfigDataModel.Constants.AccountSecretPrefix}{opsStorageAccountName}\")))";
            return mountCode;
        }
    }
    /// <summary>
    /// Kernel Properties that are entered in the blob for garbage collection
    /// </summary>
    public class KernelProperties
    {
        public DateTime CreateDate { get; set; }
        public string UserId { get; set; }
        public string FlowId { get; set; }
    }

    /// <summary>
    /// Class for creating the response Object when the kernel gets intiialized but normalization snippet was modified since the sample data was not in sync with the normalization snippet
    /// </summary>
    public class KernelResult
    {
        [JsonProperty("result")]
        public string Result { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
    }


}
