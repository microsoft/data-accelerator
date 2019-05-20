// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using DataX.Contract;
using System;
using System.Threading.Tasks;
using DataX.Utilities.Web;
using DataX.ServiceHost.AspNetCore.Authorization.Roles;

namespace Flow.LiveDataService.Controllers
{
    [Route("api")]
    [DataXWriter]
    public class LiveDataController : Controller
    {
        private readonly ILogger<LiveDataController> _logger;
        public LiveDataController(ILogger<LiveDataController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        [Route("inputdata/refreshsampleandkernel")] // diag
        public async Task<ApiResult> RefreshInputDataAndKernel([FromBody]JObject jObject)
        {
            try
            {
                RolesCheck.EnsureWriter(Request);
                LiveDataManager ldm = new LiveDataManager(_logger);
                return await ldm.RefreshInputDataAndKernel(jObject);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }
    }
}
