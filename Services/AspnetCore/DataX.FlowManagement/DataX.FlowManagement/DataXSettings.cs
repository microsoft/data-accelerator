// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataX.FlowManagement
{
    public class DataXSettings
    {
        public string EnableOneBox { get; set; }
        public string LocalRoot { get; set; }
        public string SparkHome { get; set; }
        public string MetricsHttpEndpoint { get; set; }
    }
}
