// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Flow.InteractiveQuery.Databricks
{
    /// <summary>
    /// Implementation of IKernel for Databricks stack
    /// </summary>
    public class DatabricksKernel: IKernel
    {
        private readonly string _token = string.Empty;
        private readonly string _clusterId = string.Empty;
        private readonly string _baseUrl = string.Empty;

        /// <summary>
        /// Initializes Databricks kernel
        /// </summary>
        /// <param name="id">Id of the kernel</param>
        /// <param name="baseUrl">baseUrl for the APIs of the format "https://$region.azuredatabricks.net"</param>
        /// <param name="token">User token</param>
        /// <param name="clusterId">Cluster Id</param>
        public DatabricksKernel(string id, string baseUrl, string token, string clusterId)
        {
            Id = id;
            _baseUrl = baseUrl;
            _token = token;
            _clusterId = clusterId;
        }

        /// <summary>
        /// Id of the kernel
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Executes the code and returns the results
        /// </summary>
        /// <param name="code">Code to execute</param>
        /// <returns>Results of code execution</returns>
        public string ExecuteCode(string code)
        {
            // Call service
            HttpClient client = DatabricksUtility.GetHttpClient(_token);
            var nvc = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("command", code)
            };
            var url = $"{_baseUrl}/api/1.2/commands/execute?language=scala&clusterId={_clusterId}&contextId={Id}";
            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(nvc) };
            var response = client.SendAsync(request).Result;
            var responseString = response.Content.ReadAsStringAsync().Result;
            string commandId = JsonConvert.DeserializeObject<ExecuteCodeDBKernelResponse>(responseString).Id;

            // Now get the result output
            CommandResponse result = null;
            string status = "Running";
            do
            {
                var response2 = client.GetAsync($"{_baseUrl}/api/1.2/commands/status?clusterId={_clusterId}&contextId={Id}&commandId={commandId}").Result;
                var responsestring2 = response2.Content.ReadAsStringAsync().Result;
                result = JsonConvert.DeserializeObject<CommandResponse>(responsestring2);
                status = result.Status;
            } while (status != "Finished");
            client.Dispose();

            return (result.Results.Data != null) ? result.Results.Data.ToString() : "";
        }

    }

    /// <summary>
    /// This is the object that is returned from the Kernel. This helps extract out the kernelID 
    /// </summary>
    public class ExecuteCodeDBKernelResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Result contained in the command execution
    /// </summary>
    public class Results
    {
        [JsonProperty("resultType")]
        public string ResultType { get; set; }

        [JsonProperty("data")]
        public object Data { get; set; }

        [JsonProperty("summary")]
        public object Summary { get; set; }

        [JsonProperty("cause")]
        public string Cause { get; set; }
    }

    /// <summary>
    /// Output of command execution
    /// </summary>
    public class CommandResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("results")]
        public Results Results { get; set; }
    }
}
