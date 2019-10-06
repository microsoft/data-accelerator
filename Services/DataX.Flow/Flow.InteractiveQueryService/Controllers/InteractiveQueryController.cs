// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Contract;
using DataX.Flow.Common.Models;
using DataX.Flow.InteractiveQuery;
using DataX.ServiceHost.AspNetCore.Authorization.Roles;
using DataX.Utilities.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Flow.InteractiveQueryService.Controllers
{
    [Route("api")]
    [DataXWriter]
    public class InteractiveQueryController : Controller
    {
        private readonly ILogger<InteractiveQueryController> _logger;
        private readonly IConfiguration _configuration;
        public InteractiveQueryController(ILogger<InteractiveQueryController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost]
        [Route("kernel")] // diag
        public async Task<ApiResult> CreateAndInitializeKernel([FromBody]JObject jObject)
        {
            try
            {
                RolesCheck.EnsureWriter(Request);
                InteractiveQueryManager iqm = new InteractiveQueryManager(_logger, _configuration);
                ApiResult response = await iqm.CreateAndInitializeKernel(jObject);

                //Logging information / success
                _logger.LogInformation("Successful Kernel Initialization!");
                return response;

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }

        [HttpPost]
        [Route("kernel/refresh")] // diag
        public async Task<ApiResult> RefreshKernel([FromBody]JObject jObject)
        {
            try
            {
                RolesCheck.EnsureWriter(Request);

                InteractiveQueryManager iqm = new InteractiveQueryManager(_logger, _configuration);
                return await iqm.RecycleKernel(jObject);

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }

        [HttpPost]
        [Route("kernel/deleteList")]
        public async Task<ApiResult> DeleteKernelList([FromBody]System.Collections.Generic.List<string> kernels, string flowName)
        {
            try
            {
                RolesCheck.EnsureWriter(Request);


                InteractiveQueryManager iqm = new InteractiveQueryManager(_logger, _configuration);
                return await iqm.DeleteKernelList(kernels, flowName).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }

        [HttpPost]
        [Route("kernel/delete")] // diag
        public async Task<ApiResult> DeleteKernel([FromBody]JObject jObject)
        {
            try
            {
                RolesCheck.EnsureWriter(Request);

                var diag = jObject.ToObject<InteractiveQueryObject>();
                InteractiveQueryManager iqm = new InteractiveQueryManager(_logger, _configuration);
                return await iqm.DeleteKernel(diag.KernelId, diag.Name).ConfigureAwait(false);

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }

        [HttpPost]
        [Route("kernels/delete")]
        public async Task<ApiResult> DeleteKernels([FromBody]string flowName)
        {
            try
            {
                RolesCheck.EnsureWriter(Request);

                InteractiveQueryManager iqm = new InteractiveQueryManager(_logger, _configuration);
                return await iqm.DeleteKernels(flowName).ConfigureAwait(false);

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }

        [HttpPost]
        [Route("kernels/deleteall")]
        public async Task<ApiResult> DeleteAllKernels([FromBody]string flowName)
        {
            try
            {
                RolesCheck.EnsureWriter(Request);

                InteractiveQueryManager iqm = new InteractiveQueryManager(_logger, _configuration);
                return await iqm.DeleteAllKernels(flowName).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }


        [HttpPost]
        [Route("kernel/sampleinputfromquery")] // diag
        public async Task<ApiResult> GetSampleInput([FromBody]JObject jObject)
        {
            try
            {
                RolesCheck.EnsureWriter(Request);

                InteractiveQueryManager iqm = new InteractiveQueryManager(_logger, _configuration);
                return await iqm.GetSampleInputFromQuery(jObject);

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }

        [HttpPost]
        [Route("kernel/executequery")] // diag
        public async Task<ApiResult> ExecuteCode([FromBody]JObject jObject)
        {
            try
            {
                RolesCheck.EnsureWriter(Request);
                InteractiveQueryManager iqm = new InteractiveQueryManager(_logger, _configuration);
                return await iqm.ExecuteQuery(jObject);

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }

    }
}
