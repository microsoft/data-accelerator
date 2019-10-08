// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Flow.Common;
using DataX.Flow.Common.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using DataX.Contract;
using System.Threading.Tasks;

namespace DataX.Flow.InteractiveQuery.Databricks
{
    /// <summary>
    /// Implementation of KervelService for Databricks stack. KernelService helps with creating and destroying kernels and maintiaing the life cycle of kernels.
    /// </summary>
    public class DatabricksKernelService: KernelService
    {
        private readonly string _token = string.Empty;
        private readonly string _clusterName = string.Empty;
        private string _clusterId = string.Empty;
        private string _baseUrl = "https://$region.azuredatabricks.net";

        /// <summary>
        /// Constructor for DatabricksKernerService
        /// </summary>
        /// <param name="flowConfig">Config of the flow</param>
        /// <param name="connectionInfo">Spark connection info</param>
        /// <param name="logger">Logger for results/errors</param>
        /// <param name="databricksToken">databricks token</param>
        public DatabricksKernelService(FlowConfigObject flowConfig, SparkConnectionInfo connectionInfo, ILogger logger, string databricksToken) : base(flowConfig, connectionInfo, logger)
        {
            string region = string.IsNullOrEmpty(flowConfig.SparkRegion) ? flowConfig.ResourceGroupLocation : flowConfig.SparkRegion;
            _baseUrl = _baseUrl.Replace("$region", region);
            _clusterName = flowConfig.SparkClusterName;
            _token = databricksToken;
        }

        /// <summary>
        /// CreateKernelAsync - calls into the Rest api for creating the kernel
        /// </summary>
        /// <returns>ApiResult which contains kernelid</returns>
        public override async Task<ApiResult> CreateKernelAsync()
        {
            try
            {
                // Iniitalize cluster id
                InitializeClusterId();
                
                // Call service
                HttpClient client = DatabricksUtility.GetHttpClient(_token);
                var nvc = new List<KeyValuePair<string, string>>();
                var url = $"{_baseUrl}/api/1.2/contexts/create?language=scala&clusterId={_clusterId}";
                var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(nvc) };
                var response = await client.SendAsync(request);
                var responseString = await response.Content.ReadAsStringAsync();
                string id = JsonConvert.DeserializeObject<CreateDBKernelResponse>(responseString).Id;
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
            string responseString = string.Empty;

            try
            {
                InitializeClusterId();
                // Set body
                string body = "{\"contextId\":\""+kernelId+"\", \"clusterId\":\""+_clusterId+"\"}";
                var content = new StringContent(body);

                // Application killed by user.
                HttpClient client = DatabricksUtility.GetHttpClient(_token);
                var response = await client.PostAsync($"{_baseUrl}/api/1.2/contexts/destroy", content);
                responseString = await response.Content.ReadAsStringAsync();
                string id = JsonConvert.DeserializeObject<DeleteDBKernelResponse>(responseString).Id;
                client.Dispose();
                return ApiResult.CreateSuccess(id);
            }
            catch (Exception ex)
            {
                return ApiResult.CreateError(responseString + "\n" + ex.ToString());
            }
        }


        /// <summary>
        /// Gets kernel of the given Id
        /// </summary>
        /// <param name="kernelId">Id of kernel</param>
        /// <returns>Kernel of given Id</returns>
        public override IKernel GetKernel(string kernelId)
        {
            Dictionary<string, string> hs = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {_token}" }
            };
            InitializeClusterId();
            IKernel kernel = new DatabricksKernel(kernelId, _baseUrl, _token, _clusterId);
            return kernel;
        }

        /// <summary>
        /// Initialized the cluster Id given the Spark cluster name
        /// </summary>
        private void InitializeClusterId()
        {
            if (!string.IsNullOrEmpty(_clusterId))
            {
                return;
            }

            HttpClient client = DatabricksUtility.GetHttpClient(_token);
            var response = client.GetAsync($"{_baseUrl}/api/2.0/clusters/list").Result;
            var responseString = response.Content.ReadAsStringAsync().Result;
            SparkClustersObject sparkClustersObject = JsonConvert.DeserializeObject<SparkClustersObject>(responseString);
            if (sparkClustersObject == null || sparkClustersObject.Clusters == null || sparkClustersObject.Clusters.Count <= 0)
            {
                throw new Exception("No Spark clusters found");
            }
            foreach(Cluster cluster in sparkClustersObject.Clusters)
            {
                if (cluster.Name == _clusterName)
                {
                    _clusterId = cluster.Id;
                    client.Dispose();
                    return;
                }
            }
            client.Dispose();
            throw new Exception($"No cluster with name {_clusterName} found");
        }
    }

    /// <summary>
    /// Spark cluster information
    /// </summary>
    public class Cluster
    {
        [JsonProperty("cluster_id")]
        public string Id { get; set; }

        [JsonProperty("cluster_name")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Collection of clusters in the deployment
    /// </summary>
    public class SparkClustersObject
    {
        [JsonProperty("clusters")]
        public List<Cluster> Clusters { get; set; }
    }

    /// <summary>
    /// This is the object that is returned from the Kernel. This helps extract out the kernelID 
    /// </summary>
    public class CreateDBKernelResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    /// <summary>
    /// This is the object that is returned from the Kernel. This helps extract out the kernelID 
    /// </summary>
    public class DeleteDBKernelResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class DatabricksUtility
    {
        /// <summary>
        /// Creates HttpClient with the right headers
        /// </summary>
        /// <returns></returns>
        public static HttpClient GetHttpClient(string token)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }
    }
}
