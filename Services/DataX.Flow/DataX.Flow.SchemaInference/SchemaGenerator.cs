// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.Extensions.Logging;
using DataX.Flow.Common;
using DataX.Utilities.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DataX.Flow.SchemaInference.Eventhub;
using DataX.Flow.SchemaInference.Kafka;
using DataX.Config.ConfigDataModel;
using System.Net;

namespace DataX.Flow.SchemaInference
{
    public class SchemaGenerator
    {
        private const int _DefaultDuration = 30;
        private readonly string _blobDirectory = "";
        private readonly string _checkpointContainerName = "";
        private IMessageBus _messageBus = null;
        private ILogger _logger;
        private const string _certLocation = @".\cacert.pem";
        private const string _certSource = "https://curl.haxx.se/ca/cacert.pem";

        /// <summary>
        /// Schema Generator constructor called for generating schema
        /// </summary>
        /// <param name="brokerList">BrokerList for Kafka</param>
        /// <param name="connectionString">connectionString of Messagebus</param>
        /// <param name="hubNames">List of eventhubNames or Topics (for Kafka)</param>
        /// <param name="consumerGroup">consumerGroup</param>
        /// <param name="storageConnectionString">storageConnectionString</param>
        /// <param name="checkpointContainerName">checkpointContainerName</param>
        /// <param name="blobDirectory">Destination for where samples are stored</param>
        /// <param name="inputType">Type of MessageBus</param>
        /// <param name="logger">logger for Telemetry</param>
        public SchemaGenerator(string brokerList, string connectionString, List<string> hubNames, string consumerGroup, string storageConnectionString, string checkpointContainerName, string blobDirectory, string inputType, ILogger logger)
        {
            _logger = logger;
            _blobDirectory = blobDirectory;
            _checkpointContainerName = checkpointContainerName;

            if (!File.Exists(_certLocation))
            {
                WebClient webClient = new WebClient();
                webClient.DownloadFile(_certSource, _certLocation);
            }

            if (inputType == Constants.InputType_Kafka || inputType == Constants.InputType_KafkaEventHub)
            {
                _messageBus = new KafkaMessageBus(brokerList, connectionString, hubNames, _certLocation, consumerGroup, inputType, logger);
            }
            else
            {
                _messageBus = new EventhubMessageBus(hubNames[0], consumerGroup, connectionString, storageConnectionString, checkpointContainerName, inputType, logger);
            }
            BlobHelper.DeleteAllBlobsInAContainer(storageConnectionString, checkpointContainerName, blobDirectory).Wait();
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
            EventsData eventsData = await _messageBus.GetSampleEvents(seconds <= 0 ? _DefaultDuration : seconds);
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
            EventsData eventsData = await _messageBus.GetSampleEvents(seconds <= 0 ? _DefaultDuration : seconds);
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
            BlobHelper.SaveContentToBlob(storageConnectionString, Path.Combine(samplesPath, $"{flowId}-{hashValue}.json"), content);
            BlobHelper.DeleteAllBlobsInAContainer(storageConnectionString, _checkpointContainerName, _blobDirectory).Wait();
        }

    }
}
