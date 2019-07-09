// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using System.Threading.Tasks;
using System.IO.Compression;
using System.IO;
using System.Timers;
using System.Threading;
using Newtonsoft.Json;

namespace DataX.Flow.SchemaInference.Eventhub
{
    public class EventProcessor : IEventProcessor
    {
        private static readonly Task _CompletedTask = Task.FromResult(false);
        private EventsData _eventsData = null;

        public EventProcessor(ref EventsData eventsData)
        {
            _eventsData = eventsData;
        }

        public Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            return _CompletedTask;
        }

        public Task OpenAsync(PartitionContext context)
        {
            return _CompletedTask;
        }

        public Task ProcessErrorAsync(PartitionContext context, Exception error)
        {
            return _CompletedTask;
        }

        public Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            foreach (var eventData in messages)
            {
                var data = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                EventRaw er = new EventRaw
                {
                    Raw = data
                };

                if (eventData.Properties != null && eventData.Properties.Count >= 0)
                {
                    foreach (string key in eventData.Properties.Keys)
                    {
                        er.Properties.Add(key, eventData.Properties[key].ToString());
                    }
                }

                er.SystemProperties.Add("x-opt-sequence-number", eventData.SystemProperties.SequenceNumber.ToString());
                er.SystemProperties.Add("x-opt-offset", eventData.SystemProperties.Offset.ToString());
                er.SystemProperties.Add("x-opt-enqueued-time", eventData.SystemProperties.EnqueuedTimeUtc.ToString());
                er.Json = JsonConvert.SerializeObject(er);

                _eventsData.EventsJson += er.Json + "\r\n";

                _eventsData.Events.Add(er);
            }

            return context.CheckpointAsync();
        }

    }
}
