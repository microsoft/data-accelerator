// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Flow.SchemaInference
{
    public interface IMessageBus
    {
        Task<EventsData> GetSampleEvents(int seconds);
    }
}
