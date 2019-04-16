// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Flow.SchemaInference
{
    public class EventRaw
    {
        public EventRaw()
        {
            Properties = new Dictionary<string, string>();
            SystemProperties = new Dictionary<string, string>();
        }

        public string Raw { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public Dictionary<string, string> SystemProperties { get; set; }
        public string Json { get; set; }
    }
}
