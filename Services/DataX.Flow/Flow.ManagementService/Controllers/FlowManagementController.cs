// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DataX.Config;
using DataX.Config.ConfigDataModel;
using DataX.Config.PublicService;
using DataX.Contract;
using DataX.Flow.CodegenRules;
using DataX.Flow.SqlParser;
using System;
using System.Threading.Tasks;
using System.Linq;
using DataX.Utilities.Web;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using DataX.ServiceHost.AspNetCore.Authorization.Roles;
using Microsoft.Extensions.Configuration;

namespace Flow.Management.Controllers
{
    [Route("api")]
    [DataXReader]
    public partial class FlowManagementController : Controller
    {
        private readonly ILogger<FlowManagementController> _logger;
        private readonly IConfiguration _configuration;
        private readonly FlowOperation _flowOperation;
        private JobOperation _jobOperation;
        private RuntimeConfigGeneration _runtimeConfigGenerator;
        private bool _isLocal = false;

        public FlowManagementController(ILogger<FlowManagementController> logger, IConfiguration configuration, FlowOperation flowOp, RuntimeConfigGeneration runtimeConfigGenerator, JobOperation jobOp)
        {
            _logger = logger;
            _configuration = configuration;
            _flowOperation = flowOp;
            _jobOperation = jobOp;
            _runtimeConfigGenerator = runtimeConfigGenerator;

            if (InitialConfiguration.Instance.TryGet(DataX.Config.ConfigDataModel.Constants.ConfigSettingName_EnableOneBox, out string enableOneBox))
            {
                _isLocal = enableOneBox.ToLower().Equals("true");
            }
        }

        [HttpPost]
        [Route("flow/save")] // save flow config
        [DataXWriter]
        public async Task<ApiResult> SaveFlow([FromBody]JObject config)
        {
            try
            {
                RolesCheck.EnsureWriter(Request, _isLocal);
                Ensure.NotNull(config, "config");

                var inputJson = JsonConfig.From(config.ToString());
                var result = await _flowOperation.SaveFlowConfig(FlowGuiConfig.From(inputJson));

                Ensure.IsSuccessResult(result);

                if (result.Properties != null && result.Properties.TryGetValue(FlowOperation.FlowConfigPropertyName, out object flowConfig))
                {
                    return ApiResult.CreateSuccess(JToken.FromObject(flowConfig));
                }
                else
                {
                    return ApiResult.CreateSuccess(JToken.FromObject(string.Empty));
                }

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }


        [HttpPost]
        [Route("flow/schedulebatch")] // schedule batch jobs
        public async Task<ApiResult> ScheduleBatch()
        {
            try
            {
                RolesCheck.EnsureWriter(Request, _isLocal);
                var result = await _flowOperation.ScheduleBatch(this._runtimeConfigGenerator).ConfigureAwait(false);

                return ApiResult.CreateSuccess(JToken.FromObject(result));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }

        [HttpPost]
        [Route("flow/generateconfigs")] // generate flow configs
        [DataXWriter]
        public async Task<ApiResult> GenerateConfigs([FromBody] string flowName)
        {
            try
            {
                RolesCheck.EnsureWriter(Request, _isLocal);
                Ensure.NotNull(flowName, "flowName");

                var result = await this._runtimeConfigGenerator.GenerateRuntimeConfigs(flowName);
                return ApiResult.CreateSuccess(JToken.FromObject(result));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }


        [HttpGet]
        [Route("flow/get")] // generator
        public async Task<ApiResult> GetFlow(string flowName)
        {
            try
            {
                RolesCheck.EnsureReader(Request, _isLocal);
                Ensure.NotNull(flowName, "flowName");

                var result = await _flowOperation.GetFlowByName(flowName);
                return ApiResult.CreateSuccess(JToken.FromObject(result));

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }


        [HttpGet]
        [Route("flow/getall")] // generator
        public async Task<ApiResult> GetAllFlows()
        {
            try
            {
                RolesCheck.EnsureReader(Request, _isLocal);

                var result = await _flowOperation.GetAllFlows();
                return ApiResult.CreateSuccess(JToken.FromObject(result));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }

        [HttpGet]
        [Route("flow/getall/min")] // generator
        public async Task<ApiResult> GetAllFlowsMin()
        {
            try
            {
                RolesCheck.EnsureReader(Request, _isLocal);

                var flowConfigs = await _flowOperation.GetAllFlows();
                var result = flowConfigs.Select(x => new { name = x.Name, displayName = x.DisplayName, owner = x.GetGuiConfig()?.Owner });
                return ApiResult.CreateSuccess(JToken.FromObject(result));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }


        [HttpPost]
        [Route("flow/startjobs")]
        [DataXWriter]
        public async Task<ApiResult> StartJobsForFlow([FromBody] string flowName)
        {
            try
            {
                RolesCheck.EnsureWriter(Request, _isLocal);
                Ensure.NotNull(flowName, "flowName");

                // start all jobs associated with the flow
                var result = await _flowOperation.StartJobsForFlow(flowName);
                return ApiResult.CreateSuccess(JToken.FromObject(result));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }

        [HttpPost]
        [Route("flow/restartjobs")]
        [DataXWriter]
        public async Task<ApiResult> RestartJobsForFlow([FromBody] string flowName)
        {
            try
            {
                RolesCheck.EnsureWriter(Request, _isLocal);
                Ensure.NotNull(flowName, "flowName");

                // restart all jobs for the flow
                var result = await _flowOperation.RestartJobsForFlow(flowName);
                return ApiResult.CreateSuccess(JToken.FromObject(result));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }

        [HttpPost]
        [Route("flow/stopjobs")]
        [DataXWriter]
        public async Task<ApiResult> StopJobsForFlow([FromBody] string flowName)
        {
            try
            {
                RolesCheck.EnsureWriter(Request, _isLocal);
                Ensure.NotNull(flowName, "flowName");

                // stop all jobs for flow
                var result = await _flowOperation.StopJobsForFlow(flowName);
                return ApiResult.CreateSuccess(JToken.FromObject(result));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }


        [HttpPost]
        [Route("userqueries/schema")] // generator (sqlparser)
        [DataXWriter]
        public async Task<ApiResult> GetSchema([FromBody]JObject config)
        {
            try
            {
                RolesCheck.EnsureWriter(Request, _isLocal);
                Ensure.NotNull(config, "config");
                string queries = string.Join("\n", config.GetValue("query").ToString());

                var ruleDefinitionRaw = config.Value<JArray>("rules");
                var ruleDefinitionList = ruleDefinitionRaw.ToObject<List<FlowGuiRule>>();
                var ruleDefinitions = RuleDefinitionGenerator.GenerateRuleDefinitions(ruleDefinitionList, config.GetValue("name").ToString());
                RulesCode codeGen = CodeGen.GenerateCode(queries, ruleDefinitions, config.GetValue("name").ToString());
                var result = Analyzer.Analyze(queries, codeGen.Code, config.GetValue("inputSchema").ToString());

                await Task.Yield();
                return ApiResult.CreateSuccess(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }


        [HttpPost]
        [Route("userqueries/codegen")] // generator
        [DataXWriter]
        public async Task<ApiResult> GetCodeGen([FromBody]JObject config)
        {
            try
            {
                RolesCheck.EnsureWriter(Request, _isLocal);
                Ensure.NotNull(config, "config");

                string queries = string.Join("\n", config.GetValue("query").ToString());

                var ruleDefinitionRaw = config.Value<JArray>("rules");
                var ruleDefinitionList = ruleDefinitionRaw.ToObject<List<FlowGuiRule>>();
                var ruleDefinitions = RuleDefinitionGenerator.GenerateRuleDefinitions(ruleDefinitionList, config.GetValue("name").ToString());

                RulesCode codeGen = CodeGen.GenerateCode(queries, ruleDefinitions, config.GetValue("name").ToString());

                await Task.Yield();
                return ApiResult.CreateSuccess(codeGen.Code);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }

        [HttpGet]
        [Route("job/getall")]
        public async Task<ApiResult> GetAllJobs()
        {
            try
            {
                RolesCheck.EnsureReader(Request, _isLocal);

                // Get all jobs
                var jobOpResult = await _jobOperation.GetJobs();
                return ApiResult.CreateSuccess(JToken.FromObject(jobOpResult));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }

        [HttpGet]
        [Route("job/get")]
        public async Task<ApiResult> GetJob(string jobName)
        {
            try
            {
                RolesCheck.EnsureReader(Request, _isLocal);
                Ensure.NotNull(jobName, "jobName");

                // Get a job
                var jobOpResult = await _jobOperation.GetJobsByNames(new string[] { jobName });
                return ApiResult.CreateSuccess(JToken.FromObject(jobOpResult));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }

        [HttpPost]
        [Route("job/getbynames")]
        public async Task<ApiResult> GetJobsByNames([FromBody] string[] jobNames)
        {
            try
            {
                RolesCheck.EnsureReader(Request, _isLocal);
                Ensure.NotNull(jobNames, "jobNames");

                // Get jobs for the names given
                var jobOpResult = await _jobOperation.GetJobsByNames(jobNames);
                return ApiResult.CreateSuccess(JToken.FromObject(jobOpResult));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }

        [HttpPost]
        [Route("job/start")] // start the job
        [DataXWriter]
        public async Task<ApiResult> StartJob([FromBody] string jobName)
        {
            try
            {
                RolesCheck.EnsureWriter(Request, _isLocal);
                Ensure.NotNull(jobName, "jobName");

                // Start the job
                var jobOpResult = await _jobOperation.StartJob(jobName);
                return ApiResult.CreateSuccess(JToken.FromObject(jobOpResult));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }


        [HttpPost]
        [Route("job/stop")] // stop the job
        [DataXWriter]
        public async Task<ApiResult> StopJob([FromBody] string jobName)
        {
            try
            {
                RolesCheck.EnsureWriter(Request, _isLocal);
                Ensure.NotNull(jobName, "jobName");

                // Stop the job
                var jobOpResult = await _jobOperation.StopJob(jobName);
                return ApiResult.CreateSuccess(JToken.FromObject(jobOpResult));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }

        [HttpPost]
        [Route("job/restart")]
        [DataXWriter]
        public async Task<ApiResult> RestartJob([FromBody] string jobName)
        {
            try
            {
                RolesCheck.EnsureWriter(Request, _isLocal);
                Ensure.NotNull(jobName, "jobName");

                // Restart job
                var jobOpResult = await _jobOperation.RestartJob(jobName);
                return ApiResult.CreateSuccess(JToken.FromObject(jobOpResult));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }


        [HttpGet]
        [Route("job/syncall")]
        [DataXWriter]
        public async Task<ApiResult> SyncAllJobs()
        {
            try
            {
                RolesCheck.EnsureWriter(Request, _isLocal);

                // Sync all jobs
                var jobOpResult = await _jobOperation.SyncAllJobState();
                return ApiResult.CreateSuccess(JToken.FromObject(jobOpResult));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }

        [HttpPost]
        [Route("job/syncbynames")]
        public async Task<ApiResult> SyncJobsByNames([FromBody] string[] jobNames)
        {
            try
            {
                RolesCheck.EnsureReader(Request, _isLocal);
                Ensure.NotNull(jobNames, "jobNames");

                // Sync jobs for the names given
                var jobOpResult = await _jobOperation.SyncJobStateByNames(jobNames);
                return ApiResult.CreateSuccess(JToken.FromObject(jobOpResult));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }

    }
}
