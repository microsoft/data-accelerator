// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using DataX.Contract;
using DataX.Flow.Common;
using DataX.Flow.Common.Models;
using DataX.Flow.InteractiveQuery;
using DataX.Flow.SchemaInference;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Flow.LiveDataService
{
    //TODO: Change the name to represent InteractiveQuery and generateSchema combined together
    public class LiveDataManager
    {
        private string _flowContainerName => _engineEnvironment.EngineFlowConfig.FlowContainerName;       
        private EngineEnvironment _engineEnvironment;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        public LiveDataManager(ILogger logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _engineEnvironment = new EngineEnvironment(_configuration);
        }
        public async Task<ApiResult> RefreshInputDataAndKernel(JObject jObject)
        {
            var diag = jObject.ToObject<InteractiveQueryObject>();

            var response = await _engineEnvironment.GetEnvironmentVariables();
            if (response.Error.HasValue && response.Error.Value)
            {
                _logger.LogError(response.Message);
                return ApiResult.CreateError(response.Message);
            }

            //Refresh the sample data
            SchemaInferenceManager sim = new SchemaInferenceManager(_logger, _configuration);
            response = await sim.RefreshSample(jObject);

            if (response.Error.HasValue && response.Error.Value)
            {
                _logger.LogError(response.Message);
                return ApiResult.CreateError(response.Message);
            }

            InteractiveQueryManager iqm = new InteractiveQueryManager(_logger, _configuration);
            response = await iqm.RecycleKernelHelper(diag, true);
            if (response.Error.HasValue && response.Error.Value)
            {
                _logger.LogError(response.Message);
                return ApiResult.CreateError(response.Message);
            }

            var result = response.Result;
            _logger.LogInformation("RefreshInputDataAndKernel successful!");
            return ApiResult.CreateSuccess(result);
        }
    }
}
