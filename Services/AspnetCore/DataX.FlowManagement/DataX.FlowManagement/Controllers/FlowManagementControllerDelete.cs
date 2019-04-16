// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using DataX.Contract;
using System.Threading.Tasks;
using DataX.Utilities.Web;

namespace Flow.Management.Controllers
{
    public partial class FlowManagementController
    {
        [HttpPost]
        [Route("flow/delete")] // delete flow configs
        public async Task<ApiResult> DeleteConfigs([FromBody]JObject queryObject)
        {
            try
            {
                RolesCheck.EnsureWriter(Request, _isLocal);
                Ensure.NotNull(queryObject, "queryObject");

                var flowName = queryObject.GetValue("name").ToString();

                var result = await this._runtimeConfigGenerator.DeleteConfigs(flowName);
                return ApiResult.CreateSuccess(JToken.FromObject(result));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return ApiResult.CreateError(e.Message);
            }
        }
    }
}
