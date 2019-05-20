// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using DataX.Contract;
using DataX.Flow.SchemaInference;
using System;
using System.Threading.Tasks;
using DataX.Utilities.Web;
using DataX.ServiceHost.AspNetCore.Authorization.Roles;

namespace Flow.SchemaInferenceService.Controllers
{
    [Route("api")]
    [DataXWriter]
    public class SchemaInferenceController : Controller
    {
        private readonly ILogger<SchemaInferenceController> _logger;
        public SchemaInferenceController(ILogger<SchemaInferenceController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        [Route("inputdata/inferschema")] // schemainf
        public async Task<ApiResult> GetInputSchema([FromBody]JObject jObject)
        {
            try
            {
                RolesCheck.EnsureWriter(Request);
                SchemaInferenceManager sim = new SchemaInferenceManager(_logger);
                return await sim.GetInputSchema(jObject);

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }

        [HttpPost]
        [Route("inputdata/refreshsample")]  // schema inf
        public async Task<ApiResult> RefreshSample([FromBody]JObject jObject)
        {
            try
            {
                RolesCheck.EnsureWriter(Request);
                SchemaInferenceManager sim = new SchemaInferenceManager(_logger);
                return await sim.RefreshSample(jObject);

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }

    }
}
