// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;

namespace DataX.Config.ConfigDataModel
{
    public class FlowGuiJobConfig
    {
        [JsonProperty("jobNumExecutors")]
        public string JobNumExecutors { get; set; }

        [JsonProperty("jobExecutorMemory")]
        public string JobExecutorMemory { get; set; }

        [JsonProperty("jobDatabricksAutoScale")]
        public bool JobDatabricksAutoScale { get; set; }

        [JsonProperty("jobDatabricksMinWorkers")]
        public string JobDatabricksMinWorkers { get; set; }

        [JsonProperty("jobDatabricksMaxWorkers")]
        public string JobDatabricksMaxWorkers { get; set; }
    }
}
