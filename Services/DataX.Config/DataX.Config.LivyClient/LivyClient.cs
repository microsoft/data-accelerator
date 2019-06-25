// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DataX.Config.ConfigDataModel;
using DataX.Contract;
using DataX.Contract.Exception;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.LivyClient
{
    /// <summary>
    /// Class to interact directly with the HTTP endpoint of the Livy service
    /// </summary>
    public class LivyClient : ISparkJobClient
    {
        //cluster information
        private readonly LivyClientConnectionInfo _connectionInfo;
        private readonly ILivyHttpClient _httpClient;

        private const string _LivyUrlSuffix = "/batches";

        public LivyClient(string connectionString, ILivyHttpClientFactory httpClientFactory)
        {
            _connectionInfo = ConnectionStringParser.Parse(connectionString);
            _httpClient = httpClientFactory.CreateClientWithBasicAuth(_connectionInfo.UserName, _connectionInfo.Password);
        }

        /// <summary>
        /// Get all jobs information from the cluster
        /// </summary>
        /// <param name="result">results if any from the call</param>
        /// <returns>success</returns>
        public async Task<SparkJobSyncResult[]> GetJobs()
        {
            //list: () => get('/batches'),
            var result = await CallLivyService(HttpMethod.Get, _LivyUrlSuffix);
            if (result.IsSuccess)
            {
                var batches = JsonConvert.DeserializeObject<LivyBatchesResult>(result.Content);
                return batches.Sessions.Select(ParseLivyBatchResult).ToArray();
            }
            else
            {
                throw new GeneralException($"failed to get all batches: '{result.Content}'");
            }
        }

        /// <summary>
        /// Get detailed jobid information from the cluster
        /// </summary>
        /// <param name="jobId">integer for the job</param>
        /// <returns>success</returns>
        public async Task<SparkJobSyncResult> GetJobInfo(JToken clientCache)
        {
            //getById: id => get(`/batches/${id}`),
            var batch = clientCache.ToObject<LivyBatchResult>();
            Ensure.NotNull(batch, "batch");
            var id = batch.Id;
            Ensure.NotNull(id, "batchId");

            var result = await CallLivyService(HttpMethod.Get, $"{_LivyUrlSuffix}/{batch.Id}");
            return ParseJobInfoFromLivyHttpResult(result);
        }

        /// <summary>
        /// Submit a job to the cluster
        /// </summary>
        /// <param name="jobdata">JSON data for the job</param>
        /// <param name="result">results if any from the call</param>
        /// <returns>success</returns>
        public async Task<SparkJobSyncResult> SubmitJob(JToken jobData)
        {
            //submit: options => post('/batches', options),
            Ensure.NotNull(jobData, "jobData");
            var result = await CallLivyService(HttpMethod.Post, _LivyUrlSuffix, jobData.ToString());
            return ParseJobInfoFromLivyHttpResult(result);
        }

        /// <summary>
        /// Stop/Kill a job on the server
        /// </summary>
        /// <param name="clientCache">clientCache</param>
        /// <param name="result">results if any from the call</param>
        /// <returns>success</returns>
        public async Task<SparkJobSyncResult> StopJob(JToken clientCache)
        {
            //kill: id => requestDelete(`/batches/id`)
            var batch = clientCache.ToObject<LivyBatchResult>();
            var id = batch.Id;
            Ensure.NotNull(id, "batchId");

            var httpResult = await CallLivyService(HttpMethod.Delete, $"{_LivyUrlSuffix}/{batch.Id}");

            if(httpResult.IsSuccess)
            {
                // If stop is successful reset the state to allow user start the job
                return GetDefaultSparkJobSyncResult(httpResult.Content);
            }
            else
            {
                throw new GeneralException($"Unexpected response from livy service:'{httpResult.StatusCode}', message:'{httpResult.Content}'");
            }
        }

        /// <summary>
        /// Call Livy service to manage jobs
        /// </summary>
        /// <param name="method">GET, POST or DELETE</param>
        /// <param name="jobId">Job ID, optional</param>
        /// <param name="data">Job data, required only for starting jobs</param>
        /// <returns>Result with IsSuccess and message; exception message if exception occurs</returns>
        private async Task<LivyHttpResult> CallLivyService(HttpMethod method, string api, string body = "")
        {
            //Make the call to the HTTP endpoint
            var uri = new Uri(_connectionInfo.Endpoint + api);
            return await _httpClient.ExecuteHttpRequest(method, uri, body);
        }

        public static SparkJobSyncResult ParseJobInfoFromLivyHttpResult(LivyHttpResult httpResult)
        {
            if (httpResult.IsSuccess)
            {
                try
                {
                    var batch = JsonConvert.DeserializeObject<LivyBatchResult>(httpResult.Content);
                    return ParseLivyBatchResult(batch);
                }
                catch (Exception ex)
                {
                    throw new GeneralException($"Couldn't parse response from Livy service:'{httpResult.Content}', message:'{ex.Message}'");
                }
            }
            else if(httpResult.StatusCode == HttpStatusCode.NotFound)
            {
                // if session is not found, we should reset the state to allow user start a new job
                return GetDefaultSparkJobSyncResult(httpResult.Content);
            }
            else
            {
                throw new GeneralException($"unexpected response from livy service:'{httpResult.StatusCode}', message:'{httpResult.Content}'");
            }
        }

        public static SparkJobSyncResult ParseLivyBatchResult(LivyBatchResult batchResult)
        {
            var sparkUiUrl = batchResult.AppInfo?.GetOrDefault("sparkUiUrl", null);
            var links = sparkUiUrl == null ? null : new Dictionary<string, string>()
            {
                {"App UI", sparkUiUrl},
                {"Logs", sparkUiUrl.Replace("/proxy/", "/cluster/app/")}
            };

            SparkJobSyncResult sr = new SparkJobSyncResult();

            try
            {
                var note = batchResult.Log == null ? null : string.Join("\n", batchResult.Log);
                sr.JobId = batchResult.Id.ToString();
                sr.JobState = ParseLivyBatchState(batchResult.State);
                sr.ClientCache = JObject.FromObject(batchResult);
                sr.Links = links;
                sr.Note = note;
            }
            catch (Exception ex)
            {
                throw new GeneralException($"Couldn't parse response from Livy service:'{batchResult}', message:'{ex.Message}'");
            }
            return sr;
        }

        public static JobState ParseLivyBatchState(string state)
        {
            switch (state)
            {
                case "starting": return JobState.Starting;
                case "running":return JobState.Running;
                case "dead":return JobState.Idle;
                case "success":return JobState.Success;
                default: throw new GeneralException($"Unexpected livy batch state:'{state}'");
            }
        }

        private static SparkJobSyncResult GetDefaultSparkJobSyncResult(string content="")
        {
            return new SparkJobSyncResult()
            {
                JobId = null,
                JobState = JobState.Idle,
                Note = content,
                Links = null,
                ClientCache = null
            };
        }
    }
}
