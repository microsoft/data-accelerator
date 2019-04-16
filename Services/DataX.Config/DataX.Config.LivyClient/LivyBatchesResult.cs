// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.Config.LivyClient
{
    public class LivyBatchesResult
    {
        [JsonProperty("from")]
        public int From { get; set; }

        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("sessions")]
        public LivyBatchResult[] Sessions { get; set; }
    }
}
