// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Contract;
using DataX.Contract.Exception;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.DatabricksClient
{
    /// <summary>
    /// Class to interact directly with the HTTP endpoint of the Databricks
    /// </summary>
    public class DatabricksClient : ISparkJobClient
    {
        private readonly DatabricksClientConnectionInfo _connectionInfo;
        private readonly IDatabricksHttpClient _httpClient;

        public DatabricksClient(string connectionString, IDatabricksHttpClientFactory httpClientFactory)
        {
            _connectionInfo = ConnectionStringParser.Parse(connectionString);
            _httpClient = httpClientFactory.CreateClientWithBearerToken(_connectionInfo.DbToken);
        }

        /// <summary>
        /// Call Databricks service to manage jobs
        /// </summary>
        /// <param name="method">GET, POST or DELETE</param>
        /// <param name="api">databricks api</param>
        /// <param name="body">body of httpRequest</param>
        /// <returns>Result with IsSuccess and message; exception message if exception occurs</returns>
        private async Task<DatabricksHttpResult> CallDatabricksService(HttpMethod method, string api, string body = "")
        {
            //Make the call to the HTTP endpoint
            var uri = new Uri(_connectionInfo.Endpoint + api);
            return await _httpClient.ExecuteHttpRequest(method, uri, body);
        }

        public static SparkJobSyncResult ParseJobInfoFromDatabricksHttpResult(DatabricksHttpResult httpResult)
        {
            if (httpResult.IsSuccess)
            {
                try
                {
                    var job = JsonConvert.DeserializeObject<DatabricksJobResult>(httpResult.Content);
                    return ParseDatabricksJobResult(job);
                }
                catch (Exception ex)
                {
                    throw new GeneralException($"Couldn't parse response from Databricks service:'{httpResult.Content}', message:'{ex.Message}'");
                }
            }
            else if (httpResult.StatusCode == HttpStatusCode.NotFound)
            {
                // if session is not found, we should reset the state to allow user start a new job
                return GetDefaultSparkJobSyncResult(httpResult.Content);
            }
            else
            {
                throw new GeneralException($"unexpected response from Databricks service:'{httpResult.StatusCode}', message:'{httpResult.Content}'");
            }
        }

        private static SparkJobSyncResult GetDefaultSparkJobSyncResult(string content = "")
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

        public static JobState ParseDatabricksJobState(string state)
        {
            switch (state)
            {
                case "PENDING":
                    return JobState.Starting;
                case "RUNNING":
                    return JobState.Running;
                case "INTERNAL_ERROR":
                    return JobState.Error;
                case "SKIPPED":
                case "TERMINATING":
                case "TERMINATED":
                    return JobState.Idle;
                default:
                    throw new GeneralException($"Unexpected databricks job state:'{state}'");
            }
        }

        public static SparkJobSyncResult ParseDatabricksJobResult(DatabricksJobResult jobResult)
        {
            SparkJobSyncResult sr = new SparkJobSyncResult();

            try
            {
                sr.JobId = jobResult.JobId.ToString();
                sr.JobState = ParseDatabricksJobState(jobResult.State.GetOrDefault("life_cycle_state", null));
                sr.ClientCache = JObject.FromObject(jobResult);
                sr.Note = jobResult.State.GetOrDefault("state_message", null);
            }
            catch (Exception ex)
            {
                throw new GeneralException($"Couldn't parse response from Databricks service:'{jobResult}', message:'{ex.Message}'");
            }
            return sr;
        }

        public async Task<SparkJobSyncResult> SubmitJob(JToken jobData)
        {
            Ensure.NotNull(jobData, "jobData");
            JObject obj = JObject.Parse(jobData.ToString());
            JObject newCluster = (JObject)obj["new_cluster"];
            if (!(bool)obj.SelectToken("new_cluster.enableAutoscale"))
            {
                newCluster.Property("autoscale").Remove();
            }
            else
            {
                newCluster.Property("num_workers").Remove();
            }
            var jobResult = await CallDatabricksService(HttpMethod.Post, "jobs/create", obj.ToString());
            var jobId = JsonConvert.DeserializeObject<DatabricksJobResult>(jobResult.Content).JobId;
            var runResult = await CallDatabricksService(HttpMethod.Post, "jobs/run-now", $@"{{""job_id"":{jobId}}}");
            var runId = JsonConvert.DeserializeObject<DatabricksJobResult>(runResult.Content).RunId;
            var result = await CallDatabricksService(HttpMethod.Get, "jobs/runs/get?run_id=" + runId);
            return ParseJobInfoFromDatabricksHttpResult(result);
        }

        public async Task<SparkJobSyncResult> GetJobInfo(JToken jobClientData)
        {
            var runId = JsonConvert.DeserializeObject<DatabricksJobResult>(jobClientData.ToString()).RunId;
            var result = await CallDatabricksService(HttpMethod.Get, "jobs/runs/get?run_id=" + runId);
            return ParseJobInfoFromDatabricksHttpResult(result);
        }

        public async Task<SparkJobSyncResult> StopJob(JToken jobClientData)
        {
            var clientData = JsonConvert.DeserializeObject<DatabricksJobResult>(jobClientData.ToString());
            await CallDatabricksService(HttpMethod.Post, "jobs/runs/cancel", $@"{{""run_id"":{clientData.RunId}}}");
            await CallDatabricksService(HttpMethod.Post, "jobs/delete", $@"{{""job_id"":{clientData.JobId}}}");
            var result = new SparkJobSyncResult();
            //Fetch status of job after it has been stopped
            var numRetry = 0;
            do
            {
                var jobStatus = await CallDatabricksService(HttpMethod.Get, $"jobs/runs/get?run_id={clientData.RunId}");
                result = ParseJobInfoFromDatabricksHttpResult(jobStatus);
                //When job is in progress of termination, fetch the latest job state max 5 times untill the job has stopped.
                numRetry++;
            } while (result.JobState == JobState.Running && numRetry <= 5);
            return result;
        }

        public async Task<SparkJobSyncResult[]> GetJobs()
        {
            await Task.CompletedTask;
            throw new NotImplementedException();
        }
    }
}
