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
    public class EventsData
    {
        public EventsData()
        {
            Events = new List<EventRaw>();
        }

        public List<EventRaw> Events = null;
        public string EventsJson = null;
    }
}
