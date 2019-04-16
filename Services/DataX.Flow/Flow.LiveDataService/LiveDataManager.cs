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

namespace Flow.LiveDataService
{
    //TODO: Change the name to represent InteractiveQuery and generateSchema combined together
    public class LiveDataManager
    {
        private string _flowContainerName => _engineEnvironment.EngineFlowConfig.FlowContainerName;       
        private EngineEnvironment _engineEnvironment = new EngineEnvironment();
        private readonly ILogger _logger;
        public LiveDataManager(ILogger logger)
        {
            _logger = logger;
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
            SchemaInferenceManager sim = new SchemaInferenceManager(_logger);
            response = await sim.RefreshSample(jObject);

            if (response.Error.HasValue && response.Error.Value)
            {
                _logger.LogError(response.Message);
                return ApiResult.CreateError(response.Message);
            }

            InteractiveQueryManager iqm = new InteractiveQueryManager(_logger);
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
