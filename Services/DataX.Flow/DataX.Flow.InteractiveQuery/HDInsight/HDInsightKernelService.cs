// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Contract;
using DataX.Flow.Common;
using DataX.Flow.Common.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DataX.Flow.InteractiveQuery.HDInsight
{
    /// <summary>
    /// Implementation of KernelService for HDInsight stack. KernelService creates and destroys kernels and maintains the life cycle of kernels.
    /// </summary>
    public class HDInsightKernelService : KernelService
    {
        private readonly string _username = string.Empty;
        private readonly string _password = string.Empty;
        private readonly string _token = string.Empty;
        private string _baseUrl = "https://$name.azurehdinsight.net/jupyter";

        /// <summary>
        /// Constructor for HDInisghtKernelService
        /// </summary>
        /// <param name="flowConfig">Config of floe</param>
        /// <param name="connectionInfo">Spark connection info</param>
        /// <param name="logger">Logger for errors/results</param>
        public HDInsightKernelService(FlowConfigObject flowConfig, SparkConnectionInfo connectionInfo, ILogger logger) : base(flowConfig, connectionInfo, logger)
        {
            _baseUrl = _baseUrl.Replace("$name", flowConfig.JobURLBase);
            _username = connectionInfo.UserName;
            _password = connectionInfo.Password;
            _token = Base64Encode($"{_username}:{_password}");
        }

        /// <summary>
        /// CreateKernelAsync - calls into the Rest api for creating the kernel
        /// </summary>
        /// <returns>ApiResult which contains kernelid</returns>
        public override async Task<ApiResult> CreateKernelAsync()
        {
            try
            {
                // Set body
                string body = "{\"name\":\"sparkkernel\"}";
                var content = new StringContent(body);

                // Call service
                HttpClient client = GetHttpClient();
                var response = await client.PostAsync($"{_baseUrl}/api/kernels", content);
                var responseString = await response.Content.ReadAsStringAsync();
                string id = JsonConvert.DeserializeObject<CreateHDIKernelResponse>(responseString).Id;
                client.Dispose();
                return ApiResult.CreateSuccess(id);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                return ApiResult.CreateError(ex.ToString());
            }
        }

        /// <summary>
        /// This is called for deleting the kernel by directly calling into the Rest Api's provided by the jupyter kernel
        /// </summary>
        /// <param name="kernelId">KernelId</param>
        /// <returns>Returns success or error as the case maybe as ApiResult</returns>
        public override async Task<ApiResult> DeleteKernelAsync(string kernelId)
        {
            try
            {
                if (string.IsNullOrEmpty(kernelId))
                {
                    return ApiResult.CreateSuccess("Success");
                }

                HttpClient client = GetHttpClient();
                var response = await client.DeleteAsync($"{_baseUrl}/api/kernels/{kernelId}");
                client.Dispose();
                if (response.IsSuccessStatusCode)
                {
                    return ApiResult.CreateSuccess("Success");
                }
                else
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return ApiResult.CreateError(result);
                }
            }
            catch (Exception ex)
            {
                return ApiResult.CreateError(ex.ToString());
            }
        }

        /// <summary>
        /// Gets the kernel of the desired kernelId
        /// </summary>
        /// <param name="kernelId">Id of kernel</param>
        /// <returns>Kernel for HDInsight</returns>
        public override IKernel GetKernel(string kernelId)
        {
            Dictionary<string, string> hs = new Dictionary<string, string>
            {
                { "Authorization", $"Basic {_token}" }
            };
            HDInsightKernel kernel = new HDInsightKernel(kernelId, _baseUrl, null, null, null, hs);
            return kernel;
        }

        /// <summary>
        /// Creates HttpClient with the right headers
        /// </summary>
        /// <returns>HttpClient to communicate with the Spark HDInsight cluster</returns>
        private HttpClient GetHttpClient()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _token);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header
            return client;
        }
    }

    /// <summary>
    /// This is the object that is returned from the Kernel. This helps extract out the kernelID 
    /// </summary>
    public class CreateHDIKernelResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
