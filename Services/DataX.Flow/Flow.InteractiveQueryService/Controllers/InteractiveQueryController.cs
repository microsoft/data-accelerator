// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using DataX.Contract;
//using DataX.Flow.Common;
using DataX.Flow.InteractiveQuery;
using System;
using System.Threading.Tasks;
using DataX.Utilities.Web;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Flow.InteractiveQueryService.Controllers
{
    [Route("api")]
    public class InteractiveQueryController : Controller
    {
        private readonly ILogger<InteractiveQueryController> _logger;
        public InteractiveQueryController(ILogger<InteractiveQueryController> logger)
        {
            _logger = logger;
        }
        [HttpPost]
        [Route("kernel")] // diag
        public async Task<ApiResult> CreateAndInitializeKernel([FromBody]JObject jObject)
        {
            try
            {
                RolesCheck.EnsureWriter(Request);
                InteractiveQueryManager iqm = new InteractiveQueryManager(_logger);
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

                InteractiveQueryManager iqm = new InteractiveQueryManager(_logger);
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
        public async Task<ApiResult> DeleteKernelList([FromBody]System.Collections.Generic.List<string> kernels)
        {
            try
            {
                RolesCheck.EnsureWriter(Request);


                InteractiveQueryManager iqm = new InteractiveQueryManager(_logger);
                return await iqm.DeleteKernelList(kernels).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }

        [HttpPost]
        [Route("kernel/delete")] // diag
        public async Task<ApiResult> DeleteKernel([FromBody]string kernelId)
        {
            try
            {
                RolesCheck.EnsureWriter(Request);


                InteractiveQueryManager iqm = new InteractiveQueryManager(_logger);
                return await iqm.DeleteKernel(kernelId).ConfigureAwait(false);

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }

        [HttpPost]
        [Route("kernels/delete")]
        public async Task<ApiResult> DeleteKernels()
        {
            try
            {
                RolesCheck.EnsureWriter(Request);

                InteractiveQueryManager iqm = new InteractiveQueryManager(_logger);
                return await iqm.DeleteKernels().ConfigureAwait(false);

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }

        [HttpPost]
        [Route("kernels/deleteall")]
        public async Task<ApiResult> DeleteAllKernels()
        {
            try
            {
                RolesCheck.EnsureWriter(Request);

                InteractiveQueryManager iqm = new InteractiveQueryManager(_logger);
                return await iqm.DeleteAllKernels().ConfigureAwait(false);
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

                InteractiveQueryManager iqm = new InteractiveQueryManager(_logger);
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
                InteractiveQueryManager iqm = new InteractiveQueryManager(_logger);
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
