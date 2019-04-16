// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataX.Contract
{
    public class ApiResult
    {
        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Error
        {
            get; set;
        }

        [JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
        public string Message
        {
            get; set;
        }

        [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
        public JToken Result
        {
            get; set;
        }

        public static ApiResult CreateSuccess(JToken result)
        {
            return new ApiResult() { Result = result };
        }

        public static ApiResult CreateError(string message)
        {
            return new ApiResult()
            {
                Message = message,
                Error = true
            };
        }
    }
}
