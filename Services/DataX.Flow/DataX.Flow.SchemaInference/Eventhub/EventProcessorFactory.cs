// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.Azure.EventHubs.Processor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataX.Flow.SchemaInference.Eventhub
{
    /// <summary>
    /// Factory class to enable dependency injection for <see cref="EventHubReaderDispatcher"/>.
    /// </summary>
    internal class EventProcessorFactory : IEventProcessorFactory
    {
        public EventsData EventsData = null;

        public EventProcessorFactory()
        {
            EventsData = new EventsData();
        }

        IEventProcessor IEventProcessorFactory.CreateEventProcessor(PartitionContext context)
        {
            return new EventProcessor(ref EventsData);
        }
    }

}
