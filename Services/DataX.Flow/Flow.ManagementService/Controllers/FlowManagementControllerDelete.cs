// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using DataX.Contract;
using DataX.Flow.DeleteHelper;
using DataX.Utilities.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Flow.Management.Controllers
{
    public partial class FlowManagementController : Controller
    {
        [HttpPost]
        [Route("flow/delete")] // Flow delete
        public async Task<ApiResult> DeleteFlow([FromBody]JObject queryObject)
        {
            try
            {
                RolesCheck.EnsureWriter(Request);
                Ensure.NotNull(queryObject, "queryObject");

                ConfigDeleter c = new ConfigDeleter(_logger, _configuration);
                return await c.DeleteFlow(queryObject);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }

        }
    }
}
