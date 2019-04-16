// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Logging;
using DataX.Flow.Common;
using DataX.Utilities.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataX.Flow.SchemaInference
{
    public class SchemaGenerator
    {
        private EventProcessorHost _eventProcessorHost = null;
        private readonly string _eventhubConnectionString = null;
        private readonly string _checkpointContainerName = "";
        private readonly string _storageConnectionString = "";
        private readonly string _blobDirectory = "";
        private const int _DefaultDuration = 30;
        private ILogger _logger;

        /// <summary>
        /// Schema Generator constructor called for generating schema
        /// </summary>
        /// <param name="eventhubName">eventhubName</param>
        /// <param name="consumerGroup">consumerGroup</param>
        /// <param name="eventhubConnectionString">eventhubConnectionString</param>
        /// <param name="storageConnectionString">storageConnectionString</param>
        /// <param name="checkpointContainerName">checkpointContainerName</param>
        /// <param name="logger">logger for Telemetry</param>
        public SchemaGenerator(string eventhubName, string consumerGroup, string eventhubConnectionString, string storageConnectionString, string checkpointContainerName, string blobDirectory, ILogger logger)
        {
            _logger = logger;
            _eventhubConnectionString = eventhubConnectionString;
            _storageConnectionString = storageConnectionString;
            _checkpointContainerName = checkpointContainerName;
            _blobDirectory = blobDirectory;

            BlobHelper.DeleteAllBlobsInAContainer(storageConnectionString, checkpointContainerName, blobDirectory).Wait();
            _eventProcessorHost = new EventProcessorHost(eventhubName, consumerGroup, eventhubConnectionString, storageConnectionString, checkpointContainerName);
        }

        /// <summary>
        /// Gets the schema from a sample data generated within 30 seconds as default
        /// </summary>
        /// <param name="storageConnectionString">storageConnectionString</param>
        /// <param name="samplesPath">samplesPath</param>
        /// <param name="userId">userId</param>
        /// <param name="flowId">flowId</param>
        /// <param name="seconds">seconds is the duration of time</param>
        /// <returns>Returns ApiResult with schema Result object</returns>
        public async Task<SchemaResult> GetSchemaAsync(string storageConnectionString, string samplesPath, string userId, string flowId, int seconds = _DefaultDuration)
        {
            // Get sample events
            EventsData eventsData = await GetSampleEvents(seconds);
            List<EventRaw> events = eventsData.Events;
            _logger.LogInformation($"Event count = {events.Count}");     
            
            if (events.Count < 1)
            {
                throw new Exception("Can't capture any data from the data source.");
            }

            // Save sample events
            SaveSample(storageConnectionString, samplesPath, userId, flowId, eventsData.EventsJson);
            _logger.LogInformation($"successful save to {samplesPath}");
            // Get schema

            //log schemaResult.schema.length and count of errors
            SchemaResult schemaResult = GetSchemaHelper(events);
            _logger.LogInformation($"schemaResult.schema.length: {schemaResult.Schema.Length} and count of errors: {schemaResult.Errors.Count}");
            return schemaResult;
        }

        /// <summary>
        /// RefreshSample
        /// </summary>
        /// <param name="storageConnectionString">storageConnectionString</param>
        /// <param name="samplesPath">samplesPath</param>
        /// <param name="userId">userId</param>
        /// <param name="flowId">flowId</param>
        /// <param name="seconds">seconds is duration of time for the sample</param>
        /// <returns>void</returns>
        public async Task RefreshSample(string storageConnectionString, string samplesPath, string userId, string flowId, int seconds = _DefaultDuration)
        {
            // Get sample events
            EventsData eventsData = await GetSampleEvents(seconds);
            List<EventRaw> events = eventsData.Events;
            _logger.LogInformation($"RefreshSample: events.Count = {events.Count}");

            if (events.Count < 1)
            {
                throw new Exception("Can't capture any data from the data source.");
            }

            // Save sample events
            SaveSample(storageConnectionString, samplesPath, userId, flowId, eventsData.EventsJson);
        }

        /// <summary>
        /// Gets Sample Events
        /// </summary>
        /// <param name="seconds">seconds for which the sample data is fetched</param>
        /// <returns>Returns EventsData object</returns>
        private async Task<EventsData> GetSampleEvents(int seconds = _DefaultDuration)
        {
            if (seconds <= 0)
            {
                seconds = _DefaultDuration;
            }

            // Registers the Event Processor Host and starts receiving messages
            EventProcessorFactory eventProcessorFactory = new EventProcessorFactory();
            await _eventProcessorHost.RegisterEventProcessorFactoryAsync(eventProcessorFactory, GetEventProcessorOptions());

            Thread.Sleep(seconds * 1000);

            // Disposes of the Event Processor Host
            await _eventProcessorHost.UnregisterEventProcessorAsync();

            return eventProcessorFactory.EventsData;
        }

        /// <summary>
        /// GetSchema Helper
        /// </summary>
        /// <param name="events">events</param>
        /// <returns>Returns SchemaResult</returns>
        private SchemaResult GetSchemaHelper(List<EventRaw> events)
        {
            Engine engine = new Engine();

            List<string> eventStrings = new List<string>();
            foreach (EventRaw e in events)
            {
                eventStrings.Add(e.Raw);
            }

            SchemaResult output = engine.GetSchema(eventStrings);

            return output;
        }

        /// <summary>
        /// Saves Sample on blob and deletes the checkpoint containers if they exist
        /// </summary>
        /// <param name="storageConnectionString">storageConnectionString</param>
        /// <param name="samplesPath">samplesPath</param>
        /// <param name="userId">userId</param>
        /// <param name="flowId">flowId</param>
        /// <param name="content">content</param>
        private void SaveSample(string storageConnectionString, string samplesPath, string userId, string flowId, string content)
        {
            var hashValue = Helper.GetHashCode(userId);
            BlobHelper.SaveContentToBlob(storageConnectionString, Path.Combine(samplesPath, $"{flowId}-{hashValue}.json" ), content);
            BlobHelper.DeleteAllBlobsInAContainer(storageConnectionString, _checkpointContainerName, _blobDirectory).Wait();
        }

        /// <summary>
        ///  Returns the event process options 
        /// </summary>
        private EventProcessorOptions GetEventProcessorOptions()
        {
            var options = new EventProcessorOptions()
            {
                MaxBatchSize = 500,
                PrefetchCount = 500,
                ReceiveTimeout = TimeSpan.FromSeconds(20),
                InitialOffsetProvider = (partitionId) => DateTime.UtcNow.AddSeconds(-60)  // start from 60 second back since it is possible that the data may not be flowing for the last 40 seconds or so.
            };
            return options;
        }

    }

}
